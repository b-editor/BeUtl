﻿using System.Reactive.Subjects;
using Beutl.Api;
using Beutl.Api.Services;
using Beutl.Services.PrimitiveImpls;
using Beutl.ViewModels.SettingsPages;

namespace Beutl.ViewModels;

public sealed class SettingsDialogViewModel
{
    private readonly Subject<object> _navigateRequested = new();
    private readonly Lazy<AccountSettingsPageViewModel> _account;
    private readonly Lazy<ViewSettingsPageViewModel> _view;
    private readonly Lazy<EditorSettingsPageViewModel> _editor;
    private readonly Lazy<FontSettingsPageViewModel> _font;
    private readonly Lazy<ExtensionsSettingsPageViewModel> _extensionsPage;
    private readonly Lazy<InformationPageViewModel> _information;
    private readonly Lazy<KeyMapSettingsPageViewModel> _keyMap;

    public SettingsDialogViewModel(BeutlApiApplication clients)
    {
        _account = new(() => new AccountSettingsPageViewModel(clients));
        _editor = new(() => new EditorSettingsPageViewModel());
        _view = new(() => new ViewSettingsPageViewModel(_editor));
        _font = new(() => new FontSettingsPageViewModel());
        _extensionsPage = new(() => new ExtensionsSettingsPageViewModel());
        _information = new(() => new InformationPageViewModel());
        _keyMap = new(() => new KeyMapSettingsPageViewModel(clients.GetResource<ContextCommandManager>()));
    }

    public AccountSettingsPageViewModel Account => _account.Value;

    public ViewSettingsPageViewModel View => _view.Value;

    public EditorSettingsPageViewModel Editor => _editor.Value;

    public FontSettingsPageViewModel Font => _font.Value;

    public ExtensionsSettingsPageViewModel ExtensionsPage => _extensionsPage.Value;

    public InformationPageViewModel Information => _information.Value;

    public KeyMapSettingsPageViewModel KeyMap => _keyMap.Value;

    public IObservable<object> NavigateRequested => _navigateRequested;

    public void GoToSettingsPage()
    {
        _navigateRequested.OnNext(Information);
    }

    public void GoToAccountSettingsPage()
    {
        _navigateRequested.OnNext(Account);
    }
}
