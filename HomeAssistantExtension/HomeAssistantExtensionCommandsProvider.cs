// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using HomeAssistantExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HomeAssistantExtension;

public partial class HomeAssistantExtensionCommandsProvider : CommandProvider
{
    private readonly SettingsManager _settingsManager;
    private readonly HomeAssistantService _haService;
    private readonly ICommandItem[] _commands;

    public HomeAssistantExtensionCommandsProvider()
    {
        _settingsManager = new SettingsManager();
        _haService = new HomeAssistantService(_settingsManager);

        DisplayName = "Home Assistant";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        _commands =
        [
            new CommandItem(new HomeAssistantExtensionPage(_haService, _settingsManager))
            {
                Title    = "Home Assistant",
                Subtitle = "Toggle lights, switches and other entities",
                Icon     = IconHelpers.FromRelativePath("Assets\\StoreLogo.png"),
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
