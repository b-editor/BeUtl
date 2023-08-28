﻿#pragma warning disable CS0436

using Avalonia.Controls;

using Beutl.Controls.Navigation;
using Beutl.Rendering;
using Beutl.Threading;

using Reactive.Bindings;

namespace Beutl.ViewModels.SettingsPages;

public sealed class InfomationPageViewModel : PageContext
{
    public InfomationPageViewModel()
    {
        RenderThread.Dispatcher.Dispatch(() =>
        {
            if (!Design.IsDesignMode)
            {
                _ = SharedGRContext.GetOrCreate();
                GlVersion.Value = SharedGRContext.Version;

                GpuDevice.Value = SharedGPUContext.Device.Name;

                using var sw = new StringWriter();
                SharedGPUContext.Device.PrintInformation(sw);

                GpuDeviceDetail.Value = sw.ToString();
            }
        }, DispatchPriority.Low);
    }

    public string CurrentVersion { get; } = GitVersionInformation.SemVer;

    public string BuildMetadata { get; } = GitVersionInformation.FullBuildMetaData;

    public string GitRepositoryUrl { get; } = "https://github.com/b-editor/beutl";

    public string LicenseUrl { get; } = "https://github.com/b-editor/beutl/blob/main/LICENSE";

    public string ThirdPartyNoticesUrl { get; } = "https://github.com/b-editor/beutl/blob/main/THIRD_PARTY_NOTICES.md";

    public ReactivePropertySlim<string?> GlVersion { get; } = new();

    public ReactivePropertySlim<string?> GpuDevice { get; } = new();

    public ReactivePropertySlim<string?> GpuDeviceDetail { get; } = new();
}