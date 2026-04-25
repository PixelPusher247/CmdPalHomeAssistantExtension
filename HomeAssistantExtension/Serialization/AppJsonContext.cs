using HomeAssistantExtension.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HomeAssistantExtension.Serialization;

[JsonSerializable(typeof(HaTemplateRequest))]
[JsonSerializable(typeof(HaServiceCallPayload))]
[JsonSerializable(typeof(HaStateResponse))]
[JsonSerializable(typeof(List<HaStateResponse>))]
[JsonSerializable(typeof(List<HaEntityAreaEntry>))]
[JsonSerializable(typeof(List<HaTemplateRequest>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PluginSettings))]
internal sealed partial class AppJsonContext : JsonSerializerContext { }