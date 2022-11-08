﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Xaml.Interactivity;

using Beutl.Controls;
using Beutl.Framework;
using Beutl.Framework.Service;
using Beutl.Framework.Services;
using Beutl.Models;
using Beutl.Pages;
using Beutl.ProjectSystem;
using Beutl.Services;
using Beutl.ViewModels;
using Beutl.ViewModels.Dialogs;
using Beutl.Views.Dialogs;

using DynamicData;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using Microsoft.Extensions.DependencyInjection;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Beutl.Views;

internal readonly struct Cache<T>
    where T : class
{
    public readonly T?[] Items;

    public Cache(int size)
    {
        Items = new T?[size];
    }

    public bool Set(T item)
    {
        foreach (ref T? item0 in Items.AsSpan())
        {
            if (item0 == null)
            {
                item0 = item;
                return true;
            }
        }

        return false;
    }

    public T? Get()
    {
        foreach (ref T? item in Items.AsSpan())
        {
            if (item != null)
            {
                T? tmp = item;
                item = null;
                return tmp;
            }
        }

        return null;
    }
}

public sealed partial class MainView : UserControl
{
    private static readonly Binding s_headerBinding = new("Header");
    private readonly AvaloniaList<MenuItem> _rawRecentFileItems = new();
    private readonly AvaloniaList<MenuItem> _rawRecentProjItems = new();
    private readonly Cache<MenuItem> _menuItemCache = new(4);
    private readonly CompositeDisposable _disposables = new();
    private readonly AvaloniaList<NavigationViewItem> _navigationItems = new();
    private readonly EditorService _editorService = ServiceLocator.Current.GetRequiredService<EditorService>();
    private readonly IProjectService _projectService = ServiceLocator.Current.GetRequiredService<IProjectService>();
    private readonly INotificationService _notificationService = ServiceLocator.Current.GetRequiredService<INotificationService>();
    private readonly IWorkspaceItemContainer _workspaceItemContainer = ServiceLocator.Current.GetRequiredService<IWorkspaceItemContainer>();
    private readonly Avalonia.Animation.Animation _animation = new()
    {
        Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
        Children =
        {
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 0.0),
                    new Setter(TranslateTransform.YProperty, 28.0)
                },
                Cue = new Cue(0d)
            },
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 1.0),
                    new Setter(TranslateTransform.YProperty, 0.0)
                },
                Cue = new Cue(1d)
            }
        },
        Duration = TimeSpan.FromSeconds(0.67),
        FillMode = FillMode.Forward
    };
    private IControl? _settingsView;

    public MainView()
    {
        InitializeComponent();

        // NavigationViewの設定
        Navi.MenuItems = _navigationItems;
        Navi.ItemInvoked += NavigationView_ItemInvoked;

        recentFiles.Items = _rawRecentFileItems;
        recentProjects.Items = _rawRecentProjItems;
    }

    private async void SceneSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (TryGetSelectedEditViewModel(out EditViewModel? viewModel))
        {
            var dialog = new SceneSettings()
            {
                DataContext = new SceneSettingsViewModel(viewModel.Scene)
            };
            await dialog.ShowAsync();
        }
    }

    protected override async void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _disposables.Clear();
        if (DataContext is MainViewModel viewModel)
        {
            Task task = viewModel.RunSplachScreenTask();
            _settingsView = new Pages.SettingsPage
            {
                DataContext = viewModel.SettingsPage.Context
            };
            NaviContent.Children.Clear();
            NaviContent.Children.Add(_settingsView);
            _navigationItems.Clear();
            viewModel.Pages.ForEachItem(
                (idx, item) =>
                {
                    IControl? view = null;
                    Exception? exception = null;
                    if (item.Extension.Control != null)
                    {
                        try
                        {
                            view = Activator.CreateInstance(item.Extension.Control) as IControl;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }

                    view ??= new TextBlock()
                    {
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Text = exception != null ? @$"
Error:
    {Message.CouldNotCreateInstanceOfView}
Message:
    {exception.Message}
StackTrace:
    {exception.StackTrace}
" : @$"
Error:
    {Message.CannotDisplayThisContext}
"
                    };

                    view.DataContext = item.Context;
                    view.IsVisible = false;

                    NaviContent.Children.Insert(idx, view);
                    _navigationItems.Insert(idx, new NavigationViewItem()
                    {
                        Classes = { "SideNavigationViewItem" },
                        DataContext = item,
                        [!ContentProperty] = s_headerBinding,
                        [Interaction.BehaviorsProperty] = new BehaviorCollection
                        {
                            new NavItemHelper()
                            {
                                FilledIcon = item.Extension.FilledIcon,
                                RegularIcon = item.Extension.RegularIcon,
                            }
                        }
                    });
                },
                (idx, item) =>
                {
                    (item.Context as IDisposable)?.Dispose();
                    NaviContent.Children.RemoveAt(idx);
                    _navigationItems.RemoveAt(idx);
                },
                () => throw new Exception("'MainViewModel.Pages' does not support the 'Clear' method."))
                .AddTo(_disposables);

            viewModel.SelectedPage.Subscribe(async obj =>
            {
                if (DataContext is MainViewModel viewModel)
                {
                    int idx = obj == null ? -1 : viewModel.Pages.IndexOf(obj);

                    IControl? oldControl = null;
                    for (int i = 0; i < NaviContent.Children.Count; i++)
                    {
                        if (NaviContent.Children[i] is IControl { IsVisible: true } control)
                        {
                            control.IsVisible = false;
                            oldControl = control;
                        }
                    }

                    Navi.SelectedItem = idx >= 0 ? _navigationItems[idx] : Navi.FooterMenuItems.Cast<object>().First();
                    IControl newControl = idx >= 0 ? NaviContent.Children[idx] : _settingsView;

                    newControl.IsVisible = true;
                    newControl.Opacity = 0;
                    await _animation.RunAsync((Animatable)newControl, null);
                    newControl.Opacity = 1;

                    newControl.Focus();
                }
            }).AddTo(_disposables);

            InitCommands(viewModel);

            await task;
            InitExtMenuItems(viewModel);

            InitRecentItems(viewModel);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (e.Root is Window b)
        {
            b.Opened += OnParentWindowOpened;
        }
    }

    private void OnParentWindowOpened(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Opened -= OnParentWindowOpened;
        }

        if (sender is AppWindow cw)
        {
            AppWindowTitleBar titleBar = cw.TitleBar;
            if (titleBar != null)
            {
                titleBar.ExtendsContentIntoTitleBar = true;

                Titlebar.Margin = new Thickness(titleBar.RightInset, 0, titleBar.LeftInset, 0);
                AppWindow.SetAllowInteractionInTitleBar(MenuBar, true);
            }
        }
    }

    private void NavigationView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.DataContext is MainViewModel.NavItemViewModel itemViewModel
            && DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedPage.Value = itemViewModel;
        }
    }

    private bool TryGetSelectedEditViewModel([NotNullWhen(true)] out EditViewModel? viewModel)
    {
        if (_editorService.SelectedTabItem.Value?.Context.Value is EditViewModel editViewModel)
        {
            viewModel = editViewModel;
            return true;
        }
        else
        {
            viewModel = null;
            return false;
        }
    }

    private void InitCommands(MainViewModel viewModel)
    {
        viewModel.CreateNewProject.Subscribe(async () =>
        {
            var dialog = new CreateNewProject();
            await dialog.ShowAsync();
        }).AddTo(_disposables);

        viewModel.OpenProject.Subscribe(async () =>
        {
            if (VisualRoot is Window window)
            {
                var options = new FilePickerOpenOptions
                {
                    FileTypeFilter = new FilePickerFileType[]
                    {
                        new FilePickerFileType(Strings.ProjectFile)
                        {
                            Patterns = new[] { $"*.{Constants.ProjectFileExtension}" }
                        }
                    }
                };

                var result = await window.StorageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0
                    && result[0].TryGetUri(out var uri)
                    && uri.IsFile)
                {
                    _projectService.OpenProject(uri.LocalPath);
                }
            }
        }).AddTo(_disposables);

        viewModel.OpenFile.Subscribe(async () =>
        {
            if (VisualRoot is not Window root || DataContext is not MainViewModel viewModel)
            {
                return;
            }

            var filters = new List<FilePickerFileType>();

            filters.AddRange(viewModel.GetEditorExtensions()
                .Select(e => new FilePickerFileType(e.FileTypeName)
                {
                    Patterns = e.FileExtensions.Select(x => $"*.{x}").ToList(),
                })
                .ToArray());
            var options = new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = filters
            };

            var files = await root.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
            {
                bool? addToProject = null;
                IWorkspace? project = _projectService.CurrentProject.Value;

                foreach (IStorageFile file in files)
                {
                    if (file.TryGetUri(out var uri) && uri.IsFile)
                    {
                        var path = uri.LocalPath;
                        if (project != null && _workspaceItemContainer.TryGetOrCreateItem(path, out IWorkspaceItem? item))
                        {
                            if (!addToProject.HasValue)
                            {
                                var checkBox = new CheckBox
                                {
                                    IsChecked = false,
                                    Content = Message.RememberThisChoice
                                };
                                var contentDialog = new ContentDialog
                                {
                                    PrimaryButtonText = Strings.Yes,
                                    CloseButtonText = Strings.No,
                                    DefaultButton = ContentDialogButton.Primary,
                                    Content = new StackPanel
                                    {
                                        Children =
                                        {
                                            new TextBlock
                                            {
                                                Text = Message.DoYouWantToAddThisItemToCurrentProject + "\n" + Path.GetFileName(path)
                                            },
                                            checkBox
                                        }
                                    }
                                };

                                ContentDialogResult result = await contentDialog.ShowAsync();
                                // 選択を記憶する
                                if (checkBox.IsChecked.Value)
                                {
                                    addToProject = result == ContentDialogResult.Primary;
                                }

                                if (result == ContentDialogResult.Primary)
                                {
                                    project.Items.Add(item);
                                    _editorService.ActivateTabItem(path, TabOpenMode.FromProject);
                                }
                            }
                            else if (addToProject.Value)
                            {
                                project.Items.Add(item);
                                _editorService.ActivateTabItem(path, TabOpenMode.FromProject);
                            }
                        }

                        _editorService.ActivateTabItem(path, TabOpenMode.YourSelf);
                    }
                }
            }
        }).AddTo(_disposables);

        viewModel.AddToProject.Subscribe(() =>
        {
            IWorkspace? project = _projectService.CurrentProject.Value;
            EditorTabItem? selectedTabItem = _editorService.SelectedTabItem.Value;

            if (project != null && selectedTabItem != null)
            {
                string filePath = selectedTabItem.FilePath.Value;
                if (project.Items.Any(i => i.FileName == filePath))
                    return;

                if (_workspaceItemContainer.TryGetOrCreateItem(filePath, out IWorkspaceItem? workspaceItem))
                {
                    project.Items.Add(workspaceItem);
                }
            }
        }).AddTo(_disposables);

        viewModel.RemoveFromProject.Subscribe(async () =>
        {
            IWorkspace? project = _projectService.CurrentProject.Value;
            EditorTabItem? selectedTabItem = _editorService.SelectedTabItem.Value;

            if (project != null && selectedTabItem != null)
            {
                string filePath = selectedTabItem.FilePath.Value;
                IWorkspaceItem? wsItem = project.Items.FirstOrDefault(i => i.FileName == filePath);
                if (wsItem == null)
                    return;

                var dialog = new ContentDialog
                {
                    CloseButtonText = Strings.Cancel,
                    PrimaryButtonText = Strings.OK,
                    DefaultButton = ContentDialogButton.Primary,
                    Content = Message.DoYouWantToExcludeThisItemFromProject + "\n" + filePath
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    project.Items.Remove(wsItem);
                }
            }
        }).AddTo(_disposables);

        viewModel.NewScene.Subscribe(async () =>
        {
            var dialog = new CreateNewScene();
            await dialog.ShowAsync();
        }).AddTo(_disposables);

        viewModel.AddLayer.Subscribe(async () =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.FindToolTab<TimelineViewModel>() is TimelineViewModel timeline)
            {
                var dialog = new AddLayer
                {
                    DataContext = new AddLayerViewModel(viewModel.Scene,
                        new LayerDescription(timeline.ClickedFrame, TimeSpan.FromSeconds(5), timeline.ClickedLayer))
                };
                await dialog.ShowAsync();
            }
        }).AddTo(_disposables);

        viewModel.DeleteLayer.Subscribe(async () =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.Scene is Scene scene
                && viewModel.SelectedObject.Value is Layer layer)
            {
                string name = Path.GetFileName(layer.FileName);
                var dialog = new ContentDialog
                {
                    CloseButtonText = Strings.Cancel,
                    PrimaryButtonText = Strings.OK,
                    DefaultButton = ContentDialogButton.Primary,
                    Content = Message.DoYouWantToDeleteThisFile + "\n" + name
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    scene.RemoveChild(layer).Do();
                    if (File.Exists(layer.FileName))
                    {
                        File.Delete(layer.FileName);
                    }
                }
            }
        }).AddTo(_disposables);

        viewModel.ExcludeLayer.Subscribe(() =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.Scene is Scene scene
                && viewModel.SelectedObject.Value is Layer layer)
            {
                scene.RemoveChild(layer).DoAndRecord(CommandRecorder.Default);
            }
        }).AddTo(_disposables);

        viewModel.CutLayer.Subscribe(async () =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.Scene is Scene scene
                && viewModel.SelectedObject.Value is Layer layer)
            {
                IClipboard? clipboard = Application.Current?.Clipboard;
                if (clipboard != null)
                {
                    JsonNode jsonNode = new JsonObject();
                    layer.WriteToJson(ref jsonNode);
                    string json = jsonNode.ToJsonString(JsonHelper.SerializerOptions);
                    var data = new DataObject();
                    data.Set(DataFormats.Text, json);
                    data.Set(Constants.Layer, json);

                    await clipboard.SetDataObjectAsync(data);
                    scene.RemoveChild(layer).DoAndRecord(CommandRecorder.Default);
                }
            }
        }).AddTo(_disposables);

        viewModel.CopyLayer.Subscribe(async () =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.Scene is Scene scene
                && viewModel.SelectedObject.Value is Layer layer)
            {
                IClipboard? clipboard = Application.Current?.Clipboard;
                if (clipboard != null)
                {
                    JsonNode jsonNode = new JsonObject();
                    layer.WriteToJson(ref jsonNode);
                    string json = jsonNode.ToJsonString(JsonHelper.SerializerOptions);
                    var data = new DataObject();
                    data.Set(DataFormats.Text, json);
                    data.Set(Constants.Layer, json);

                    await clipboard.SetDataObjectAsync(data);
                }
            }
        }).AddTo(_disposables);

        viewModel.PasteLayer.Subscribe(() =>
        {
            if (TryGetSelectedEditViewModel(out EditViewModel? viewModel)
                && viewModel.FindToolTab<TimelineViewModel>() is TimelineViewModel timeline)
            {
                timeline.Paste.Execute();
            }
        }).AddTo(_disposables);

        viewModel.Exit.Subscribe(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime applicationLifetime)
            {
                applicationLifetime.Shutdown();
            }
        }).AddTo(_disposables);
    }

    private void InitExtMenuItems(MainViewModel viewModel)
    {
        if (toolTabMenuItem.Items is not IList items1)
        {
            items1 = new AvaloniaList<object>();
            toolTabMenuItem.Items = items1;
        }

        // Todo: Extensionの実行時アンロードの実現時、
        //       ForEachItemメソッドを使うかeventにする
        foreach (ToolTabExtension item in viewModel.GetToolTabExtensions())
        {
            if (item.Header == null)
                continue;

            var menuItem = new MenuItem()
            {
                Header = item.Header,
                DataContext = item
            };

            menuItem.Click += (s, e) =>
            {
                if (_editorService.SelectedTabItem.Value?.Context.Value is IEditorContext editorContext
                    && s is MenuItem { DataContext: ToolTabExtension ext }
                    && ext.TryCreateContext(editorContext, out IToolContext? toolContext))
                {
                    bool result = editorContext.OpenToolTab(toolContext);
                    if (!result)
                    {
                        toolContext.Dispose();
                    }
                }
            };

            items1.Add(menuItem);
        }

        if (editorTabMenuItem.Items is not IList items2)
        {
            items2 = new AvaloniaList<object>();
            editorTabMenuItem.Items = items2;
        }

        viewMenuItem.SubmenuOpened += (s, e) =>
        {
            EditorTabItem? selectedTab = _editorService.SelectedTabItem.Value;
            if (selectedTab != null)
            {
                foreach (MenuItem item in items2.OfType<MenuItem>())
                {
                    if (item.DataContext is EditorExtension editorExtension)
                    {
                        item.IsVisible = editorExtension.IsSupported(selectedTab.FilePath.Value);
                    }
                }
            }
        };

        foreach (EditorExtension item in viewModel.GetEditorExtensions())
        {
            var menuItem = new MenuItem()
            {
                Header = item.DisplayName,
                DataContext = item,
                IsVisible = false
            };

            if (item.Icon != null)
            {
                menuItem.Icon = new PathIcon
                {
                    Data = item.Icon,
                    Width = 18,
                    Height = 18,
                };
            }

            menuItem.Click += async (s, e) =>
            {
                EditorTabItem? selectedTab = _editorService.SelectedTabItem.Value;
                if (s is MenuItem { DataContext: EditorExtension editorExtension } menuItem
                    && selectedTab != null)
                {
                    IKnownEditorCommands? commands = selectedTab.Commands.Value;
                    if (commands != null)
                    {
                        await commands.OnSave();
                    }

                    string file = selectedTab.FilePath.Value;
                    if (editorExtension.TryCreateContext(file, out IEditorContext? context))
                    {
                        selectedTab.Context.Value.Dispose();
                        selectedTab.Context.Value = context;
                    }
                    else
                    {
                        _notificationService.Show(new Notification(
                            Title: Message.ContextNotCreated,
                            Message: string.Format(
                                format: Message.CouldNotOpenFollowingFileWithExtension,
                                arg0: editorExtension.DisplayName,
                                arg1: selectedTab.FileName.Value)));
                    }
                }
            };

            items2.Add(menuItem);
        }
    }

    private void InitRecentItems(MainViewModel viewModel)
    {
        void AddItem(AvaloniaList<MenuItem> list, string item, ReactiveCommand<string> command)
        {
            MenuItem menuItem = _menuItemCache.Get() ?? new MenuItem();
            menuItem.Command = command;
            menuItem.CommandParameter = item;
            menuItem.Header = item;
            list.Add(menuItem);
        }

        void RemoveItem(AvaloniaList<MenuItem> list, string item)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                MenuItem menuItem = list[i];
                if (menuItem.Header is string header && header == item)
                {
                    list.Remove(menuItem);
                    _menuItemCache.Set(menuItem);
                }
            }
        }

        viewModel.RecentFileItems.ForEachItem(
            item => AddItem(_rawRecentFileItems, item, viewModel.OpenRecentFile),
            item => RemoveItem(_rawRecentFileItems, item),
            () => _rawRecentFileItems.Clear())
            .AddTo(_disposables);

        viewModel.RecentProjectItems.ForEachItem(
            item => AddItem(_rawRecentProjItems, item, viewModel.OpenRecentProject),
            item => RemoveItem(_rawRecentProjItems, item),
            () => _rawRecentProjItems.Clear())
            .AddTo(_disposables);
    }
}