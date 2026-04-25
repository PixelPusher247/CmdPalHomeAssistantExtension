using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HomeAssistantExtension.Models;

/// <summary>Raw state object from GET /api/states</summary>
public class HaStateResponse
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = "unknown";

    [JsonPropertyName("attributes")]
    public HaAttributes Attributes { get; set; } = new();
}

public class HaAttributes
{
    [JsonPropertyName("friendly_name")]
    public string? FriendlyName { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("current_position")]
    public int? CurrentPosition { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? Extra { get; set; }
}

/// <summary>Payload for POST /api/template</summary>
public class HaTemplateRequest
{
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;
}

/// <summary>One entry in the area-map template response.</summary>
public class HaEntityAreaEntry
{
    [JsonPropertyName("e")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("a")]
    public string AreaName { get; set; } = string.Empty;
}

/// <summary>Payload for POST /api/services/{domain}/{service}</summary>
public class HaServiceCallPayload
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;
}
