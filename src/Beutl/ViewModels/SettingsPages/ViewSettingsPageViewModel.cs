﻿
using Beutl.Configuration;

using Reactive.Bindings;

namespace Beutl.ViewModels.SettingsPages;

public sealed class ViewSettingsPageViewModel
{
    private readonly ViewConfig _config;

    public ViewSettingsPageViewModel()
    {
        _config = GlobalConfiguration.Instance.ViewConfig;
        SelectedTheme.Value = (int)_config.Theme;
        SelectedTheme.Subscribe(v => _config.Theme = (ViewConfig.ViewTheme)v);

        IsMicaEnabled.Value = _config.IsMicaEffectEnabled;
        IsMicaEnabled.Subscribe(v => _config.IsMicaEffectEnabled = v);

        SelectedLanguage.Value = _config.UICulture;
        SelectedLanguage.Subscribe(ci => _config.UICulture = ci);
    }

    public ReactivePropertySlim<int> SelectedTheme { get; } = new();

    public ReactivePropertySlim<bool> IsMicaEnabled { get; } = new();

    public ReactivePropertySlim<CultureInfo> SelectedLanguage { get; } = new();

    public IEnumerable<CultureInfo> Cultures { get; } = LocalizeService.Instance.SupportedCultures();
}