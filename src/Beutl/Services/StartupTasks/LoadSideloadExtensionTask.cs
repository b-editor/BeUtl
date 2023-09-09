﻿using System.Collections.Concurrent;

using Avalonia.Controls;
using Avalonia.Threading;

using Beutl.Api.Services;

using FluentAvalonia.UI.Controls;

using OpenTelemetry.Trace;

using Serilog;

namespace Beutl.Services.StartupTasks;

public sealed class LoadSideloadExtensionTask : StartupTask
{
    private readonly ILogger _logger = Log.ForContext<LoadSideloadExtensionTask>();
    private readonly PackageManager _manager;

    public LoadSideloadExtensionTask(PackageManager manager)
    {
        _manager = manager;
        Task = Task.Run(async () =>
        {
            using (Activity? activity = Telemetry.StartActivity("LoadSideloadExtensionTask.Run"))
            {
                // .beutl/sideloads/ 内のパッケージを読み込む
                if (_manager.GetSideLoadPackages() is { Count: > 0 } sideloads)
                {
                    activity?.AddEvent(new ActivityEvent("Done_GetSideLoadPackages"));

                    if (await ShowDialog(sideloads))
                    {
                        activity?.AddEvent(new ActivityEvent("Loading_SideLoadPackages"));

                        Parallel.ForEach(sideloads, item =>
                        {
                            try
                            {
                                _manager.Load(item);
                            }
                            catch (Exception e)
                            {
                                activity?.RecordException(e);
                                _logger.Error(e, "Failed to load package");
                                Failures.Add((item, e));
                            }
                        });

                        activity?.AddEvent(new ActivityEvent("Loaded_SideLoadPackages"));
                    }
                }
            }
        });
    }

    public override Task Task { get; }

    public ConcurrentBag<(LocalPackage, Exception)> Failures { get; } = new();

    private static async ValueTask<bool> ShowDialog(IReadOnlyList<LocalPackage> sideloads)
    {
        await App.WaitWindowOpened();
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = Message.DoYouWantToLoadSideloadExtensions,
                Content = new ListBox
                {
                    ItemsSource = sideloads.Select(x => x.Name).ToArray(),
                    SelectedIndex = 0
                },
                PrimaryButtonText = Strings.Yes,
                CloseButtonText = Strings.No,
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        });
    }
}
