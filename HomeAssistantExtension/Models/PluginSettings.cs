// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

namespace HomeAssistantExtension.Models;

internal sealed class PluginSettings
{
    public string HaUrl { get; set; } = string.Empty;
    public bool EnableLight { get; set; } = true;
    public bool EnableSwitch { get; set; } = true;
    public bool EnableFan { get; set; }
    public bool EnableCover { get; set; }
    public bool EnableMediaPlayer { get; set; }
    public bool EnableClimate { get; set; }
    public bool EnableInputBoolean { get; set; }
    public bool EnableAutomation { get; set; }
    public bool EnableScript { get; set; }
    public bool EnableLock { get; set; }
}