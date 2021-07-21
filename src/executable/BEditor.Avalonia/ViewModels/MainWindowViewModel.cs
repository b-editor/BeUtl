﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Threading;

using BEditor.Command;
using BEditor.Data;
using BEditor.Drawing;
using BEditor.Extensions;
using BEditor.Models;
using BEditor.Primitive;
using BEditor.Primitive.Objects;
using BEditor.Properties;
using BEditor.Views;
using BEditor.Views.DialogContent;

using Microsoft.Extensions.Logging;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditor.ViewModels
{
    public class MainWindowViewModel
    {
        public static readonly MainWindowViewModel Current = new();

        public MainWindowViewModel()
        {
            Open.Subscribe(async () =>
            {
                var dialog = new OpenFileRecord
                {
                    Filters =
                    {
                        new(Strings.ProjectFile, new[] { "bedit" }),
                        new(Strings.BackupFile, new[] { "backup" }),
                    }
                };
                var service = App.FileDialog;

                if (await service.ShowOpenFileDialogAsync(dialog))
                {
                    ProgressDialog? ndialog = null;
                    try
                    {
                        ndialog = new ProgressDialog
                        {
                            IsIndeterminate = { Value = true }
                        };
                        ndialog.Show(BEditor.App.GetMainWindow());

                        await DirectOpenAsync(dialog.FileName);
                    }
                    catch (Exception e)
                    {
                        Debug.Fail(string.Empty);
                        App.Project = null;
                        App.AppStatus = Status.Idle;

                        var msg = string.Format(Strings.FailedToLoad, Strings.Project);
                        App.Message.Snackbar(msg);

                        BEditor.App.Logger?.LogError(e, msg);
                    }
                    finally
                    {
                        ndialog?.Close();
                    }
                }
            });

            Save.Select(_ => App.Project)
                .Where(p => p is not null)
                .Subscribe(async p => await p!.SaveAsync());

            SaveAs.Select(_ => App.Project)
                .Where(p => p is not null)
                .Subscribe(async p =>
                {
                    var record = new SaveFileRecord
                    {
                        DefaultFileName = (p!.Name is not null) ? p.Name + ".bedit" : "新しいプロジェクト.bedit",
                        Filters =
                        {
                            new(Strings.ProjectFile, new[] { "bedit" }),
                        }
                    };

                    if (await AppModel.Current.FileDialog.ShowSaveFileDialogAsync(record))
                    {
                        await p.SaveAsync(record.FileName);
                    }
                });

            Close.Select(_ => App)
                .Where(app => app.Project is not null)
                .Subscribe(app =>
                {
                    app.Project?.Unload();
                    app.Project = null;
                    app.AppStatus = Status.Idle;
                });

            Shutdown.Subscribe(() => BEditor.App.Shutdown(0));

            Undo.Where(_ => CommandManager.Default.CanUndo)
                .Subscribe(async _ =>
                {
                    CommandManager.Default.Undo();

                    await AppModel.Current.Project!.PreviewUpdateAsync();
                    AppModel.Current.AppStatus = Status.Edit;
                });

            Redo.Where(_ => CommandManager.Default.CanRedo)
                .Subscribe(async _ =>
                {
                    CommandManager.Default.Redo();

                    await AppModel.Current.Project!.PreviewUpdateAsync();
                    AppModel.Current.AppStatus = Status.Edit;
                });

            Remove.Where(_ => App.Project is not null)
                .Select(_ => App.Project!.CurrentScene.SelectItem)
                .Where(c => c is not null)
                .Subscribe(clip => clip!.Parent.RemoveClip(clip).Execute());

            Copy.Where(_ => App.Project is not null)
                .Select(_ => App.Project!.CurrentScene.SelectItem)
                .Where(clip => clip is not null)
                .Subscribe(async clip =>
                {
                    await using var memory = new MemoryStream();
                    await Serialize.SaveToStreamAsync(clip!, memory);

                    var json = Encoding.Default.GetString(memory.ToArray());
                    await Application.Current.Clipboard.SetTextAsync(json);
                });

            Cut.Where(_ => App.Project is not null)
                .Select(_ => App.Project!.CurrentScene.SelectItem)
                .Where(clip => clip is not null)
                .Subscribe(async clip =>
                {
                    clip!.Parent.RemoveClip(clip).Execute();

                    await using var memory = new MemoryStream();
                    await Serialize.SaveToStreamAsync(clip, memory);

                    var json = Encoding.Default.GetString(memory.ToArray());
                    await Application.Current.Clipboard.SetTextAsync(json);
                });

            Paste.Where(_ => App.Project is not null)
                .Select(_ => App.Project!.CurrentScene.GetCreateTimelineViewModel())
                .Subscribe(async timeline =>
                {
                    var mes = AppModel.Current.Message;
                    var clipboard = Application.Current.Clipboard;
                    var text = await clipboard.GetTextAsync();
                    await using var memory = new MemoryStream();
                    await memory.WriteAsync(Encoding.Default.GetBytes(text));

                    if (await Serialize.LoadFromStreamAsync<ClipElement>(memory) is var clip && clip is not null)
                    {
                        var length = clip.Length;
                        clip.Start = timeline.ClickedFrame;
                        clip.End = length + timeline.ClickedFrame;

                        clip.Layer = timeline.ClickedLayer;

                        if (!timeline.Scene.InRange(clip.Start, clip.End, clip.Layer))
                        {
                            mes?.Snackbar(Strings.ClipExistsInTheSpecifiedLocation);
                            BEditor.App.Logger.LogInformation("{0} Start: {0} End: {1} Layer: {2}", Strings.ClipExistsInTheSpecifiedLocation, clip.Start, clip.End, clip.Layer);

                            return;
                        }

                        timeline.Scene.AddClip(clip).Execute();
                    }
                    else if (File.Exists(text))
                    {
                        var start = timeline.ClickedFrame;
                        var end = timeline.ClickedFrame + 180;
                        var layer = timeline.ClickedLayer;
                        var ext = Path.GetExtension(text);

                        if (!timeline.Scene.InRange(start, end, layer))
                        {
                            mes?.Snackbar(Strings.ClipExistsInTheSpecifiedLocation);
                            return;
                        }

                        if (ext is ".bobj")
                        {
                            var efct = await Serialize.LoadFromFileAsync<EffectWrapper>(text);
                            if (efct?.Effect is not ObjectElement obj)
                            {
                                mes?.Snackbar(Strings.FailedToLoad);
                                return;
                            }

                            obj.Load();
                            obj.UpdateId();
                            timeline.Scene.AddClip(start, layer, obj, out _).Execute();
                        }
                        else
                        {
                            var supportedObjects = ObjectMetadata.LoadedObjects
                                .Where(i => i.IsSupported is not null && i.CreateFromFile is not null && i.IsSupported(text))
                                .ToArray();
                            var result = supportedObjects.FirstOrDefault();

                            if (supportedObjects.Length > 1)
                            {
                                var dialog = new SelectObjectMetadata
                                {
                                    Metadatas = supportedObjects,
                                    Selected = result,
                                };

                                result = await dialog.ShowDialog<ObjectMetadata?>(BEditor.App.GetMainWindow());
                            }

                            if (result is not null)
                            {
                                timeline.Scene.AddClip(start, layer, result.CreateFromFile!.Invoke(text), out _).Execute();
                            }
                        }
                    }
                });

            IsOpened.Subscribe(v =>
            {
                CommandManager.Default.Clear();

                if (v)
                {
                    App.RaiseProjectOpened(App.Project);
                }
            });

            ImageOutput.Where(_ => App.Project is not null).Subscribe(async _ =>
            {
                var scene = AppModel.Current.Project.CurrentScene!;

                var record = new SaveFileRecord
                {
                    Filters =
                    {
                        new(Strings.ImageFile, ImageFile.SupportExtensions)
                    }
                };

                if (await AppModel.Current.FileDialog.ShowSaveFileDialogAsync(record))
                {
                    using var img = scene.Render(ApplyType.Image);

                    img.Save(record.FileName);
                }
            });

            VideoOutput.Where(_ => App.Project is not null).Subscribe(async _ =>
            {
                var dialog = new VideoOutput();
                await dialog.ShowDialog(BEditor.App.GetMainWindow());
            });

            Previewer = new(IsOpened);
        }

        public ReactiveCommand Open { get; } = new();

        public ReactiveCommand Save { get; } = new();

        public ReactiveCommand SaveAs { get; } = new();

        public ReactiveCommand Close { get; } = new();

        public ReactiveCommand Shutdown { get; } = new();

        public ReactiveCommand Undo { get; } = new();

        public ReactiveCommand Redo { get; } = new();

        public ReactiveCommand Remove { get; } = new();

        public ReactiveCommand Cut { get; } = new();

        public ReactiveCommand Copy { get; } = new();

        public ReactiveCommand Paste { get; } = new();

        public ReactiveCommand New { get; } = new();

        public ReactiveCommand ImageOutput { get; } = new();

        public ReactiveCommand VideoOutput { get; } = new();

        public ReadOnlyReactivePropertySlim<bool> IsOpened { get; } = AppModel.Current
            .ObserveProperty(p => p.Project)
            .Select(p => p is not null)
            .ToReadOnlyReactivePropertySlim();

        public PreviewerViewModel Previewer { get; }

        public AppModel App { get; } = AppModel.Current;

        public static async ValueTask DirectOpenAsync(string filename)
        {
            var app = AppModel.Current;
            app.Project?.Unload();
            var project = Project.FromFile(filename, app);

            if (project is null) return;

            await Task.Run(() =>
            {
                project.Load();

                app.Project = project;
                app.AppStatus = Status.Edit;

                BEditor.Settings.Default.RecentFiles.Remove(filename);
                BEditor.Settings.Default.RecentFiles.Add(filename);
            });
        }
    }
}