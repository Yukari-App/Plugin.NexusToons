using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record CategoryAttr([property: JsonPropertyName("name")] string Name);
