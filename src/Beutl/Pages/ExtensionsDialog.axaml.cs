﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Beutl.Logging;
using Beutl.Pages.ExtensionsPages;
using Beutl.Pages.ExtensionsPages.DevelopPages;
using Beutl.Pages.ExtensionsPages.DiscoverPages;
using Beutl.ViewModels;
using Beutl.ViewModels.ExtensionsPages;
using Beutl.ViewModels.ExtensionsPages.DiscoverPages;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.Logging;

namespace Beutl.Pages;

public sealed partial class ExtensionsDialog : AppWindow
{
    private readonly ILogger _logger = Log.CreateLogger<ExtensionsDialog>();

    public ExtensionsDialog()
    {
        InitializeComponent();
        if (OperatingSystem.IsWindows())
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 40;
        }
        else if (OperatingSystem.IsMacOS())
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
        }

        List<NavigationViewItem> items = GetItems();
        nav.MenuItemsSource = items;
        NavigationViewItem selected = items[0];

        frame.Navigated += Frame_Navigated;
        frame.Navigating += Frame_Navigating;
        nav.ItemInvoked += Nav_ItemInvoked;
        nav.BackRequested += Nav_BackRequested;

        nav.SelectedItem = selected;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (nav.SelectedItem is NavigationViewItem selected)
        {
            OnItemInvoked(selected);
        }
    }

    private void Search_Click(object? sender, RoutedEventArgs e)
    {
        frame.Navigate(typeof(SearchPage), searchTextBox.Text);
    }

    private async void OpenSettings_Click(object? sender, RoutedEventArgs e)
    {
        var mainViewModel = Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
            ? lifetime.MainWindow!.DataContext as MainViewModel
            : null;
        if (mainViewModel is null) return;

        var dialogViewModel = mainViewModel.SettingsDialog;
        var dialog = new SettingsDialog { DataContext = dialogViewModel };
        dialogViewModel.GoToAccountSettingsPage();
        await dialog.ShowDialog(this);
    }

    private static List<NavigationViewItem> GetItems()
    {
        return
        [
            new NavigationViewItem()
            {
                Content = "Home",
                Tag = typeof(DiscoverPage),
                IconSource = new SymbolIconSource { Symbol = Symbol.Home }
            },
            new NavigationViewItem()
            {
                Content = "Library",
                Tag = typeof(LibraryPage),
                IconSource = new SymbolIconSource { Symbol = Symbol.Library }
            },
            new NavigationViewItem()
            {
                Content = "Develop",
                Tag = typeof(DevelopPage),
                IconSource = new SymbolIconSource { Symbol = Symbol.Code }
            }
        ];
    }

    private void Nav_BackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        frame.GoBack();
    }

    private void Nav_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem nvi)
        {
            OnItemInvoked(nvi);
        }
    }

    private void OnItemInvoked(NavigationViewItem nvi)
    {
        if (nvi.Tag is Type typ
            && DataContext is ExtensionsDialogViewModel { IsAuthorized.Value: true } viewModel)
        {
            NavigationTransitionInfo transitionInfo = SharedNavigationTransitionInfo.Instance;
            if (typ == typeof(DevelopPage))
            {
                frame.Navigate(typ, viewModel.Develop, transitionInfo);
            }
            else if (typ == typeof(LibraryPage))
            {
                frame.Navigate(typ, viewModel.Library, transitionInfo);
            }
            else if (typ == typeof(DiscoverPage))
            {
                frame.Navigate(typ, viewModel.Discover, transitionInfo);
            }
        }
    }

    private void Frame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        _logger.LogInformation("Navigate to '{PageName}'.", e.SourcePageType.Name);
        Type type1 = frame.CurrentSourcePageType;
        Type type2 = e.SourcePageType;

        if (type1 == type2
            && frame.Content is Control { DataContext: ISupportRefreshViewModel { Refresh: { } refreshCommand } }
            && refreshCommand.CanExecute())
        {
            refreshCommand.Execute();
        }

        if (e.NavigationTransitionInfo is EntranceNavigationTransitionInfo entrance)
        {
            if (e.NavigationMode is NavigationMode.Back)
            {
                entrance.FromHorizontalOffset = -28;
            }
            else if (e.NavigationMode is NavigationMode.Forward or NavigationMode.Refresh)
            {
                entrance.FromHorizontalOffset = 28;
            }
            else
            {
                int num1 = ToNumber(type1);
                int num2 = ToNumber(type2);
                if (num1 > num2)
                {
                    entrance.FromHorizontalOffset = -28;
                }
                else
                {
                    entrance.FromHorizontalOffset = 28;
                }
            }

            entrance.FromVerticalOffset = 0;
        }
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        foreach (NavigationViewItem nvi in nav.MenuItems.OfType<NavigationViewItem>())
        {
            if (nvi.Tag is Type tag && tag == e.SourcePageType)
            {
                nav.SelectedItem = nvi;
                return;
            }
        }

        foreach (NavigationViewItem nvi in nav.MenuItems.OfType<NavigationViewItem>())
        {
            if (nvi.Tag is Type tag && e.SourcePageType.Namespace?.EndsWith($"{tag.Name}s") == true)
            {
                nav.SelectedItem = nvi;
                return;
            }
        }
    }

    private static int ToNumber(Type type)
    {
        if (type == typeof(DevelopPage)
            || type == typeof(DiscoverPage))
        {
            return 0;
        }
        else if (type == typeof(PackageDetailsPage)
                 || type == typeof(PublicPackageDetailsPage)
                 || type == typeof(RankingPageViewModel))
        {
            return 1;
        }
        else if (type == typeof(PackageReleasesPage))
        {
            return 2;
        }
        else if (type == typeof(PackageSettingsPage))
        {
            return 2;
        }
        else if (type == typeof(ReleasePage))
        {
            return 3;
        }
        else
        {
            return -1;
        }
    }
}
