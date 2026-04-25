using HomeAssistantExtension.Models;
using HomeAssistantExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace HomeAssistantExtension;

internal sealed partial class SettingsPage : ContentPage
{
    private const string KeyHaUrl = "haUrl";
    private const string KeyToken = "accessToken";
    private const string KeyLight = "enableLight";
    private const string KeySwitch = "enableSwitch";
    private const string KeyFan = "enableFan";
    private const string KeyCover = "enableCover";
    private const string KeyMediaPlayer = "enableMediaPlayer";
    private const string KeyClimate = "enableClimate";
    private const string KeyInputBoolean = "enableInputBoolean";
    private const string KeyAutomation = "enableAutomation";
    private const string KeyScript = "enableScript";
    private const string KeyLock = "enableLock";

    private readonly Settings _settings = new();
    private readonly SettingsManager _settingsManager;
    private readonly HomeAssistantService _haService;
    private readonly Action<EntityItem?> _onSettingsSaved;

    public SettingsPage(SettingsManager settingsManager, HomeAssistantService haService, Action<EntityItem?> onSettingsSaved)
    {
        _settingsManager = settingsManager;
        _haService = haService;
        _onSettingsSaved = onSettingsSaved;

        Name = "Settings";
        Icon = new IconInfo("\uE713");
        Title = "Home Assistant Settings";

        _settings.Add(new TextSetting(KeyHaUrl, _settingsManager.HaUrl)
        {
            Label = "Home Assistant URL",
            Description = "e.g. http://homeassistant.local:8123",
        });

        _settings.Add(new TextSetting(KeyToken, SettingsManager.AccessToken)
        {
            Label = "Long-Lived Access Token",
            Description = "Generate one in HA under Profile → Long-Lived Access Tokens",
        });

        _settings.Add(new ToggleSetting(KeyLight, _settingsManager.EnableLight)
        {
            Label = "Lights",
        });

        _settings.Add(new ToggleSetting(KeySwitch, _settingsManager.EnableSwitch)
        {
            Label = "Switches",
        });

        _settings.Add(new ToggleSetting(KeyFan, _settingsManager.EnableFan)
        {
            Label = "Fans",
        });

        _settings.Add(new ToggleSetting(KeyCover, _settingsManager.EnableCover)
        {
            Label = "Covers",
        });

        _settings.Add(new ToggleSetting(KeyMediaPlayer, _settingsManager.EnableMediaPlayer)
        {
            Label = "Media Players",
        });

        _settings.Add(new ToggleSetting(KeyClimate, _settingsManager.EnableClimate)
        {
            Label = "Climate",
        });

        _settings.Add(new ToggleSetting(KeyInputBoolean, _settingsManager.EnableInputBoolean)
        {
            Label = "Input Booleans",
        });

        _settings.Add(new ToggleSetting(KeyAutomation, _settingsManager.EnableAutomation)
        {
            Label = "Automations",
        });

        _settings.Add(new ToggleSetting(KeyScript, _settingsManager.EnableScript)
        {
            Label = "Scripts",
        });

        _settings.Add(new ToggleSetting(KeyLock, _settingsManager.EnableLock)
        {
            Label = "Locks",
        });

        _settings.SettingsChanged += OnSettingsChanged;
    }

    public override IContent[] GetContent() => _settings.ToContent();

    private void OnSettingsChanged(object? sender, Settings args)
    {
        try
        {
            if (_settings.TryGetSetting<string>(KeyHaUrl, out var url) && url is not null)
                _settingsManager.HaUrl = url;

            if (_settings.TryGetSetting<string>(KeyToken, out var token) && token is not null)
                SettingsManager.AccessToken = token;

            if (_settings.TryGetSetting<bool>(KeyLight, out var light))
                _settingsManager.EnableLight = light;

            if (_settings.TryGetSetting<bool>(KeySwitch, out var sw))
                _settingsManager.EnableSwitch = sw;

            if (_settings.TryGetSetting<bool>(KeyFan, out var fan))
                _settingsManager.EnableFan = fan;

            if (_settings.TryGetSetting<bool>(KeyCover, out var cover))
                _settingsManager.EnableCover = cover;

            if (_settings.TryGetSetting<bool>(KeyMediaPlayer, out var mp))
                _settingsManager.EnableMediaPlayer = mp;

            if (_settings.TryGetSetting<bool>(KeyClimate, out var climate))
                _settingsManager.EnableClimate = climate;

            if (_settings.TryGetSetting<bool>(KeyInputBoolean, out var ib))
                _settingsManager.EnableInputBoolean = ib;

            if (_settings.TryGetSetting<bool>(KeyAutomation, out var auto))
                _settingsManager.EnableAutomation = auto;

            if (_settings.TryGetSetting<bool>(KeyScript, out var script))
                _settingsManager.EnableScript = script;

            if (_settings.TryGetSetting<bool>(KeyLock, out var lck))
                _settingsManager.EnableLock = lck;

            _haService.ReloadSettings();
            _onSettingsSaved(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Failed to save settings: {ex.Message}");
        }
    }
}
