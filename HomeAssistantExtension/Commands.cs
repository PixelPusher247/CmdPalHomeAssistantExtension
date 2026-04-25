// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using HomeAssistantExtension.Models;
using HomeAssistantExtension.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAssistantExtension;

/// <summary>Toggles a HA entity (on→off or off→on).</summary>
public partial class ToggleEntityCommand : InvokableCommand
{
    private readonly HomeAssistantService _haService;
    private readonly Action<EntityItem?>? _onToggled;
    private EntityItem _entity;

    public ToggleEntityCommand(HomeAssistantService haService, EntityItem entity, Action<EntityItem?>? onToggled = null)
    {
        _haService = haService;
        _entity = entity;
        _onToggled = onToggled;
        Name = $"Toggle {entity.FriendlyName}";
    }

    internal void UpdateEntity(EntityItem entity) => _entity = entity;

    public override CommandResult Invoke()
    {
        var entity = _entity;
        _ = Task.Run(async () =>
        {
            try
            {
                var updated = await _haService.ToggleAsync(entity, CancellationToken.None);
                _onToggled?.Invoke(updated);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HA] Toggle failed: {ex.Message}");
                _onToggled?.Invoke(null);
            }
        });
        return CommandResult.KeepOpen();
    }
}

/// <summary>Explicitly turns a HA entity on or off.</summary>
public partial class SetEntityStateCommand : InvokableCommand
{
    private readonly HomeAssistantService _haService;
    private readonly EntityItem _entity;
    private readonly bool _turnOn;
    private readonly Action<EntityItem?>? _onExecuted;

    public SetEntityStateCommand(HomeAssistantService haService, EntityItem entity, bool turnOn, Action<EntityItem?>? onExecuted = null, string? name = null)
    {
        _haService = haService;
        _entity = entity;
        _turnOn = turnOn;
        _onExecuted = onExecuted;
        Name = name ?? (turnOn ? $"Turn on {entity.FriendlyName}" : $"Turn off {entity.FriendlyName}");
    }

    public override CommandResult Invoke()
    {
        var entity = _entity;
        _ = Task.Run(async () =>
        {
            try
            {
                var updated = _turnOn
                    ? await _haService.TurnOnAsync(entity, CancellationToken.None)
                    : await _haService.TurnOffAsync(entity, CancellationToken.None);
                _onExecuted?.Invoke(updated);
            }
            catch (Exception ex)
            {
                var verb = _turnOn ? "turn on" : "turn off";
                System.Diagnostics.Debug.WriteLine($"[HA] Failed to {verb} {entity.FriendlyName}: {ex.Message}");
                _onExecuted?.Invoke(null);
            }
        });
        return CommandResult.KeepOpen();
    }
}
