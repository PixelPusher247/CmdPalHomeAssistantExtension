using HomeAssistantExtension.Models;
using HomeAssistantExtension.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAssistantExtension.Services;

public class HomeAssistantService
{
    private readonly SettingsManager _settings;
    private HttpClient _http;

    public HomeAssistantService(SettingsManager settings)
    {
        _settings = settings;
        _http = BuildClient();
    }

    public void ReloadSettings()
    {
        _http.Dispose();
        _http = BuildClient();
    }

    public async Task<List<EntityItem>> GetToggleableEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var allowedDomains = _settings.EnabledDomains;

        var states = await GetAsync("api/states", AppJsonContext.Default.ListHaStateResponse, cancellationToken) ?? [];
        var areaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var areaCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            areaCts.CancelAfter(TimeSpan.FromSeconds(10));
            areaMap = await GetEntityAreaMapAsync(areaCts.Token) ?? areaMap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HA] Area resolution failed: {ex.Message}");
        }

        return states
            .Where(s => allowedDomains.Contains(s.EntityId.Split('.')[0]))
            .Select(s =>
            {
                areaMap.TryGetValue(s.EntityId, out var areaName);
                return MapToEntityItem(s, string.IsNullOrEmpty(areaName) ? null : areaName);
            })
            .ToList();
    }

    public async Task<EntityItem?> GetEntityStateAsync(string entityId, string? areaName = null, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync($"api/states/{entityId}", AppJsonContext.Default.HaStateResponse, cancellationToken);
        return response != null ? MapToEntityItem(response, areaName) : null;
    }

    private async Task<Dictionary<string, string>?> GetEntityAreaMapAsync(CancellationToken cancellationToken)
    {
        const string template =
            "{% set ns = namespace(r=[]) %}" +
            "{% for s in states %}" +
            "{% set a = area_name(s.entity_id) %}" +
            "{% if a %}" +
            "{% set ns.r = ns.r + [{'e': s.entity_id, 'a': a}] %}" +
            "{% endif %}" +
            "{% endfor %}" +
            "{{ ns.r | tojson }}";

        var response = await _http.PostAsJsonAsync("api/template",
            new HaTemplateRequest { Template = template },
            AppJsonContext.Default.HaTemplateRequest,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var entries = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListHaEntityAreaEntry);

        return entries?.ToDictionary(e => e.EntityId, e => e.AreaName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<EntityItem> ToggleAsync(EntityItem entity, CancellationToken cancellationToken = default)
    {
        var updated = await CallServiceAsync(entity.Domain, entity.ToggleService, entity.EntityId, entity.AreaName, cancellationToken);
        return updated ?? entity.WithState(entity.IsOn ? "off" : "on");
    }

    public async Task<EntityItem> TurnOnAsync(EntityItem entity, CancellationToken cancellationToken = default)
    {
        var updated = await CallServiceAsync(entity.Domain, entity.TurnOnService, entity.EntityId, entity.AreaName, cancellationToken);
        return updated ?? entity.WithState("on");
    }

    public async Task<EntityItem> TurnOffAsync(EntityItem entity, CancellationToken cancellationToken = default)
    {
        var updated = await CallServiceAsync(entity.Domain, entity.TurnOffService, entity.EntityId, entity.AreaName, cancellationToken);
        return updated ?? entity.WithState("off");
    }

    private async Task<EntityItem?> CallServiceAsync(string domain, string service, string entityId, string? areaName, CancellationToken cancellationToken)
    {
        var payload = new HaServiceCallPayload { EntityId = entityId };
        var response = await _http.PostAsJsonAsync(
            $"api/services/{domain}/{service}",
            payload,
            AppJsonContext.Default.HaServiceCallPayload,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var changed = await response.Content.ReadFromJsonAsync(
            AppJsonContext.Default.ListHaStateResponse,
            cancellationToken: cancellationToken);
        var match = changed?.FirstOrDefault(s => string.Equals(s.EntityId, entityId, StringComparison.OrdinalIgnoreCase));
        return match is not null ? MapToEntityItem(match, areaName) : null;
    }

    private static EntityItem MapToEntityItem(HaStateResponse s, string? areaName = null) => new()
    {
        EntityId = s.EntityId,
        FriendlyName = s.Attributes.FriendlyName ?? s.EntityId,
        State = s.State,
        Position = s.Attributes.CurrentPosition,
        AreaName = areaName,
        Icon = s.Attributes.Icon,
    };

    private async Task<T?> GetAsync<T>(string relativeUrl, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken: cancellationToken);
    }

    private HttpClient BuildClient()
    {
        var client = new HttpClient
        {
            BaseAddress = string.IsNullOrWhiteSpace(_settings.HaUrl)
                ? null
                : new Uri(_settings.HaUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10),
        };

        if (!string.IsNullOrWhiteSpace(SettingsManager.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SettingsManager.AccessToken);
        }

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
