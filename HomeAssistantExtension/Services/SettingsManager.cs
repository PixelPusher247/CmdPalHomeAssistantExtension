// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using HomeAssistantExtension.Models;
using HomeAssistantExtension.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.Security.Credentials;

namespace HomeAssistantExtension.Services;

/// <summary>
/// Persists plugin settings to %LocalAppData%\HomeAssistantExtension\settings.json.
/// The Long-Lived Access Token is stored in Windows Credential Manager (PasswordVault)
/// </summary>
public class SettingsManager
{
    private const string VaultResource = "HomeAssistantExtension";
    private const string VaultUsername = "AccessToken";

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "HomeAssistantExtension");

    public static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private PluginSettings _settings;

    public SettingsManager()
    {
        _settings = Load();
    }

    public string HaUrl
    {
        get => _settings.HaUrl;
        set { _settings.HaUrl = value.TrimEnd('/'); Save(); }
    }

    public static string AccessToken
    {
        get
        {
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, VaultUsername);
                cred.RetrievePassword();
                return cred.Password;
            }
            catch
            {
                return string.Empty;
            }
        }
        set
        {
            try
            {
                var vault = new PasswordVault();
                try
                {
                    var existing = vault.Retrieve(VaultResource, VaultUsername);
                    vault.Remove(existing);
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(value))
                    vault.Add(new PasswordCredential(VaultResource, VaultUsername, value.Trim()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HA] Failed to store token in Credential Manager: {ex.Message}");
            }
        }
    }

    public bool EnableLight { get => _settings.EnableLight; set { _settings.EnableLight = value; Save(); } }
    public bool EnableSwitch { get => _settings.EnableSwitch; set { _settings.EnableSwitch = value; Save(); } }
    public bool EnableFan { get => _settings.EnableFan; set { _settings.EnableFan = value; Save(); } }
    public bool EnableCover { get => _settings.EnableCover; set { _settings.EnableCover = value; Save(); } }
    public bool EnableMediaPlayer { get => _settings.EnableMediaPlayer; set { _settings.EnableMediaPlayer = value; Save(); } }
    public bool EnableClimate { get => _settings.EnableClimate; set { _settings.EnableClimate = value; Save(); } }
    public bool EnableInputBoolean { get => _settings.EnableInputBoolean; set { _settings.EnableInputBoolean = value; Save(); } }
    public bool EnableAutomation { get => _settings.EnableAutomation; set { _settings.EnableAutomation = value; Save(); } }
    public bool EnableScript { get => _settings.EnableScript; set { _settings.EnableScript = value; Save(); } }
    public bool EnableLock { get => _settings.EnableLock; set { _settings.EnableLock = value; Save(); } }

    public HashSet<string> EnabledDomains
    {
        get
        {
            var d = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_settings.EnableLight) d.Add("light");
            if (_settings.EnableSwitch) d.Add("switch");
            if (_settings.EnableFan) d.Add("fan");
            if (_settings.EnableCover) d.Add("cover");
            if (_settings.EnableMediaPlayer) d.Add("media_player");
            if (_settings.EnableClimate) d.Add("climate");
            if (_settings.EnableInputBoolean) d.Add("input_boolean");
            if (_settings.EnableAutomation) d.Add("automation");
            if (_settings.EnableScript) d.Add("script");
            if (_settings.EnableLock) d.Add("lock");
            return d;
        }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(HaUrl) &&
        !string.IsNullOrWhiteSpace(AccessToken);

    private static PluginSettings Load()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);

            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.PluginSettings) ?? new PluginSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Failed to load settings: {ex.Message}");
        }

        var defaults = new PluginSettings();
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(defaults, AppJsonContext.Default.PluginSettings);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Failed to create default settings: {ex.Message}");
        }
        return defaults;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(_settings, AppJsonContext.Default.PluginSettings);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Failed to save settings: {ex.Message}");
        }
    }
}
