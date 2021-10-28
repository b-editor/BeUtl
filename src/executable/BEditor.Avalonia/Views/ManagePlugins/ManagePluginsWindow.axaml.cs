using System;
using System.Collections;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using BEditor.Controls;
using BEditor.LangResources;
using BEditor.Packaging;
using BEditor.ViewModels.ManagePlugins;

using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

namespace BEditor.Views.ManagePlugins
{
    public sealed class ManagePluginsWindow : FluentWindow
    {
        private readonly NavigationView _navView;
        private readonly Frame _frame;

        public ManagePluginsWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _navView = this.Find<NavigationView>("NavView");
            _frame = this.Find<Frame>("FrameView");

            _navView.BackRequested += NavView_BackRequested;
            _navView.ItemInvoked += NavView_ItemInvoked;

            AddNavigationViewMenuItems();
            _frame.Navigated += OnFrameNavigated;

            _frame.Navigate(typeof(Search));
        }

        public ManagePluginsWindow Navigate(Type type, object? parameter)
        {
            _frame.Navigate(type, parameter);
            return this;
        }

        private static NavigationViewItem? GetNVIFromPageSourceType(IEnumerable items, Type t)
        {
            foreach (var item in items)
            {
                if (item is NavigationViewItem nvi)
                {
                    if (nvi.MenuItems?.Count() > 0)
                    {
                        var inner = GetNVIFromPageSourceType(nvi.MenuItems, t);
                        if (inner == null)
                            continue;

                        return inner;
                    }
                    else if (nvi.Tag is Type tag && tag == t)
                    {
                        return nvi;
                    }
                }
            }

            return null;
        }

        private static NavigationViewItem? GetSearchItem(IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is NavigationViewItem nvi
                    && nvi.Tag is Type type
                    && type == typeof(Search))
                {
                    return nvi;
                }
            }

            return null;
        }

        private void NavView_BackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
        {
            _frame?.GoBack();
        }

        private void OnFrameNavigated(object? sender, NavigationEventArgs e)
        {
            // 検索/任意のパッケージ
            if (e.SourcePageType == typeof(PackageView)
                && GetSearchItem(_navView.MenuItems) is NavigationViewItem viewItem
                && _frame.Content is PackageView view
                && e.Parameter is Package model)
            {
                _navView.SelectedItem = viewItem;
                _navView.AlwaysShowHeader = true;
                _navView.Header = model.Name;
                view.DataContext = null;
                view.DataContext = new PackageViewModel(model);
                return;
            }

            // 検索
            if (e.SourcePageType == typeof(Search)
                && GetSearchItem(_navView.MenuItems) is NavigationViewItem viewItem2
                && _frame.Content is Search view2
                && view2.DataContext is SearchViewModel viewModel
                && e.Parameter is string pre)
            {
                _navView.SelectedItem = viewItem2;
                _navView.AlwaysShowHeader = false;
                _navView.Header = viewItem2.Content;

                viewModel.SearchText.Value = pre;
                return;
            }

            var nvi = GetNVIFromPageSourceType(_navView.MenuItems, e.SourcePageType);
            if (nvi != null)
            {
                _navView.SelectedItem = nvi;
                _navView.AlwaysShowHeader = false;
                _navView.Header = nvi.Content;
            }
        }

        private void AddNavigationViewMenuItems()
        {
            _navView.MenuItems = new List<NavigationViewItemBase>
            {
                new NavigationViewItem
                {
                    Content = Strings.Search,
                    Icon = new SymbolIcon { Symbol = Symbol.Library },
                    Tag = typeof(Search)
                },
                new NavigationViewItem
                {
                    Content = Strings.Installed,
                    Icon = new SymbolIcon { Symbol = Symbol.Download },
                    Tag = typeof(LoadedPlugins)
                },
                new NavigationViewItem
                {
                    Content = Strings.Changes,
                    Icon = new SymbolIcon { Symbol = Symbol.Calendar },
                    Tag = typeof(ScheduleChanges)
                },
                new NavigationViewItem
                {
                    Content = Strings.Update,
                    Icon = new SymbolIcon { Symbol = Symbol.Refresh },
                    Tag = typeof(Update)
                },
                new NavigationViewItem
                {
                    Content = Strings.CreatePluginPackage,
                    Icon = new SymbolIcon { Symbol = Symbol.ZipFolder },
                    Tag = typeof(CreatePluginPackage)
                },
                //new NavigationViewItem
                //{
                //    Content = Strings.User,
                //    Icon = new SymbolIcon { Symbol = Symbol.People },
                //    Tag = typeof(User)
                //},
            };
        }

        private void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            if (e.InvokedItemContainer is NavigationViewItem nvi && nvi.Tag is Type typ)
            {
                _frame.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}