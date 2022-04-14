using System.Diagnostics.CodeAnalysis;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using BeUtl.Collections;
using BeUtl.Configuration;
using BeUtl.Framework;
using BeUtl.ViewModels;
using BeUtl.Views;
using BeUtl.Views.Dialogs;

using FAPathIconSource = FluentAvalonia.UI.Controls.PathIconSource;
using FATabView = FluentAvalonia.UI.Controls.TabView;
using FATabViewItem = FluentAvalonia.UI.Controls.TabViewItem;

namespace BeUtl.Pages;

public sealed partial class EditPage : UserControl
{
    private readonly AvaloniaList<FATabViewItem> _tabItems = new();
    private IDisposable? _disposable0;

    public EditPage()
    {
        InitializeComponent();

        tabview.TabItems = _tabItems;
    }

    public bool TryGetTabItem(string file, [NotNullWhen(true)] out FATabViewItem? result)
    {
        result = _tabItems.FirstOrDefault(i => i.Content is IEditor editor && editor.EdittingFile == file);

        return result != null;
    }

    public bool TryGetTabItem(string file, [NotNullWhen(true)] out EditPageViewModel.TabViewModel? result)
    {
        if (DataContext is EditPageViewModel viewModel)
        {
            result = viewModel.TabItems.FirstOrDefault(i => i.FilePath == file);

            return result != null;
        }
        else
        {
            result = null;
            return false;
        }
    }

    public void SelectOrAddTabItem(string? file)
    {
        if (File.Exists(file) && DataContext is EditPageViewModel viewModel)
        {
            ViewConfig viewConfig = GlobalConfiguration.Instance.ViewConfig;
            viewConfig.UpdateRecentFile(file);

            if (TryGetTabItem(file, out EditPageViewModel.TabViewModel? tabItem))
            {
                tabItem.IsSelected.Value = true;
            }
            else
            {
                EditorExtension? ext = PackageManager.Instance.ExtensionProvider.MatchEditorExtension(file);

                if (ext != null)
                {
                    viewModel.TabItems.Add(new EditPageViewModel.TabViewModel(file, ext)
                    {
                        IsSelected =
                        {
                            Value = true
                        }
                    });
                }
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is EditPageViewModel viewModel)
        {
            _disposable0?.Dispose();
            _tabItems.Clear();
            _disposable0 = viewModel.TabItems.ForEachItem(
                (idx, item) =>
                {
                    EditorExtension ext = item.Extension;
                    // この内部でProject.Children.Addしているので二重に追加される
                    if (ext.TryCreateEditor(item.FilePath, out IEditor? editor))
                    {
                        var tabItem = new FATabViewItem
                        {
                            [!FATabViewItem.HeaderProperty] = new Binding("FileName"),
                            [!ListBoxItem.IsSelectedProperty] = new Binding("IsSelected.Value", BindingMode.TwoWay),
                            DataContext = item,
                            Content = editor
                        };

                        if (ext.Icon != null)
                        {
                            tabItem.IconSource = new FAPathIconSource()
                            {
                                Data = ext.Icon,
                            };
                        }

                        tabItem.CloseRequested += (s, _) =>
                        {
                            if (s is FATabViewItem { DataContext: EditPageViewModel.TabViewModel itemViewModel } && DataContext is EditPageViewModel viewModel)
                            {
                                viewModel.TabItems.Remove(itemViewModel);
                            }
                        };

                        _tabItems.Insert(idx, tabItem);
                    }
                },
                (idx, item) =>
                {
                    if (_tabItems[idx] is FATabViewItem { Content: IEditor editor })
                    {
                        editor.Close();
                        item.Dispose();
                        _tabItems.RemoveAt(idx);
                    }
                },
                () =>
                {
                    for (int i = 0; i < _tabItems.Count; i++)
                    {
                        if (_tabItems[i] is FATabViewItem { Content: IEditor editor, DataContext: EditPageViewModel.TabViewModel itemViewModel })
                        {
                            editor.Close();
                            itemViewModel.Dispose();
                        }
                    }

                    _tabItems.Clear();
                });
        }
    }

    // '開く'がクリックされた
    private void OpenClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindAncestorOfType<MainView>().DataContext is MainViewModel vm &&
            vm.OpenFile.CanExecute())
        {
            vm.OpenFile.Execute();
        }
    }

    // '新規作成'がクリックされた
    private async void NewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditPageViewModel vm) return;

        if (vm.IsProjectOpened.Value)
        {
            var dialog = new CreateNewScene();
            await dialog.ShowAsync();
        }
        else
        {
            var dialog = new CreateNewProject();
            await dialog.ShowAsync();
        }
    }

#pragma warning disable RCS1163, IDE0060
    public void AddButtonClick(FATabView? sender, EventArgs e)
#pragma warning restore RCS1163, IDE0060
    {
        if (Resources["AddButtonFlyout"] is MenuFlyout flyout)
        {
            flyout.ShowAt(tabview, true);
        }
    }
}
