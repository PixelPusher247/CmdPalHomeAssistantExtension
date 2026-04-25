// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using HomeAssistantExtension.Models;
using HomeAssistantExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAssistantExtension;

internal sealed partial class HomeAssistantExtensionPage : ListPage
{
    private readonly HomeAssistantService _haService;
    private readonly SettingsManager _settingsManager;
    private readonly SettingsPage _settingsPage;

    private List<EntityItem> _cachedEntities = [];
    private readonly Dictionary<string, (ListItem Item, ToggleEntityCommand Cmd)> _itemCache = [];
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private bool _isFetching;

    public HomeAssistantExtensionPage(HomeAssistantService haService, SettingsManager settingsManager)
    {
        _haService = haService;
        _settingsManager = settingsManager;
        _settingsPage = new SettingsPage(settingsManager, haService, RefreshAfterToggle);

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Home Assistant";
        Name = "Open";
    }

    public void RefreshAfterToggle(EntityItem? updatedEntity)
    {
        if (updatedEntity == null) IsLoading = true;
        _ = RefreshAfterToggleAsync(updatedEntity);
    }

    private async Task RefreshAfterToggleAsync(EntityItem? updatedEntity)
    {
        try
        {
            if (updatedEntity != null)
                UpdateEntityInPlace(updatedEntity);
            else
            {
                _lastRefresh = DateTime.MinValue;
                await FetchEntitiesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Refresh failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            RaiseItemsChanged();
        }
    }

    private void UpdateEntityInPlace(EntityItem entity)
    {
        var index = _cachedEntities.FindIndex(e => e.EntityId == entity.EntityId);
        if (index >= 0)
        {
            _cachedEntities[index] = entity;
            _lastRefresh = DateTime.UtcNow;
        }
        else
        {
            _lastRefresh = DateTime.MinValue;
        }
    }

    public override IListItem[] GetItems()
    {
        var settingsItem = new ListItem(_settingsPage)
        {
            Title = "Settings",
            Subtitle = _settingsManager.IsConfigured
                ? $"Connected to {_settingsManager.HaUrl}"
                : "Not configured — tap to set your HA URL and token",
            Icon = new IconInfo("\uE713"),
        };

        if (!_settingsManager.IsConfigured)
            return [settingsItem];

        RefreshCacheIfNeeded();

        var entityItems = _cachedEntities
            .OrderBy(e => e.Domain)
            .ThenBy(e => e.FriendlyName)
            .Select(BuildListItem);

        return [.. entityItems, settingsItem];
    }

    private ListItem BuildListItem(EntityItem entity)
    {
        var icon = GetEntityIcon(entity);
        var title = entity.AreaName is not null ? $"{entity.AreaName} {entity.FriendlyName}" : entity.FriendlyName;
        var tags = BuildTags(entity);

        if (_itemCache.TryGetValue(entity.EntityId, out var cached))
        {
            cached.Cmd.UpdateEntity(entity);
            cached.Item.Title = title;
            cached.Item.Subtitle = entity.StateLabel;
            cached.Item.Icon = icon;
            cached.Item.Tags = [.. tags];
            return cached.Item;
        }

        var cmd = new ToggleEntityCommand(_haService, entity, e => RefreshAfterToggle(e));
        var item = new ListItem(cmd)
        {
            Title = title,
            Subtitle = entity.StateLabel,
            Icon = icon,
            Tags = [.. tags],
            MoreCommands = BuildMoreCommands(entity),
        };
        _itemCache[entity.EntityId] = (item, cmd);
        return item;
    }

    private CommandContextItem[] BuildMoreCommands(EntityItem entity)
    {
        if (entity.Domain == "script")
        {
            return [new CommandContextItem(new SetEntityStateCommand(_haService, entity, true, RefreshAfterToggle, "Run")) { Title = "Run" }];
        }

        var onLabel = entity.Domain switch
        {
            "cover" => "Open",
            "lock" => "Unlock",
            "media_player" => "Power on",
            _ => "Turn on",
        };
        var offLabel = entity.Domain switch
        {
            "cover" => "Close",
            "lock" => "Lock",
            "media_player" => "Power off",
            _ => "Turn off",
        };

        return
        [
            new CommandContextItem(new SetEntityStateCommand(_haService, entity, true,  RefreshAfterToggle, onLabel))  { Title = onLabel,  Icon = GetDomainIcon(entity, on: true) },
            new CommandContextItem(new SetEntityStateCommand(_haService, entity, false, RefreshAfterToggle, offLabel)) { Title = offLabel, Icon = GetDomainIcon(entity, on: false) },
        ];
    }

    private static IconInfo GetDomainIcon(EntityItem entity, bool on)
    {
        var assetName = entity.Domain switch
        {
            "light" => on ? "light-on.png" : "light-off.png",
            "switch" => on ? "switch-on.png" : "switch-off.png",
            "fan" => on ? "fan-on.png" : "fan-off.png",
            "media_player" => on ? "media-on.png" : "media-off.png",
            _ => on ? "on.png" : "off.png",
        };
        return IconHelpers.FromRelativePath($"Assets\\{assetName}");
    }

    private static List<Tag> BuildTags(EntityItem entity)
    {
        var tags = new List<Tag> { new() { Text = entity.EntityId } };
        if (entity.AreaName is not null)
            tags.Insert(0, new Tag { Text = char.ToUpperInvariant(entity.Domain[0]) + entity.Domain[1..] });
        return tags;
    }

    private static IconInfo GetEntityIcon(EntityItem entity)
    {
        var assetName = entity.Domain switch
        {
            "light" => entity.IsOn ? "light-on.png" : "light-off.png",
            "switch" => entity.IsOn ? "switch-on.png" : "switch-off.png",
            "fan" => entity.IsOn ? "fan-on.png" : "fan-off.png",
            "media_player" => entity.IsOn ? "media-on.png" : "media-off.png",
            "climate" => "climate.png",
            "cover" => "cover.png",
            _ => "StoreLogo.png",
        };
        return IconHelpers.FromRelativePath($"Assets\\{assetName}");
    }

    private async void RefreshCacheIfNeeded()
    {
        if (DateTime.UtcNow - _lastRefresh < CacheDuration)
            return;

        IsLoading = true;
        await FetchEntitiesAsync();
        IsLoading = false;
        RaiseItemsChanged();
    }

    private async Task FetchEntitiesAsync()
    {
        if (_isFetching) return;
        _isFetching = true;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _cachedEntities = (await _haService.GetToggleableEntitiesAsync(cts.Token)) ?? [];
            _itemCache.Clear();
            _lastRefresh = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Fetch failed: {ex.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }
}
