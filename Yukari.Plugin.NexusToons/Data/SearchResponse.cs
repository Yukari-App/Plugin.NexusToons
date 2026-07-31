using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record SearchResponse([property: JsonPropertyName("data")] NexusToonsComic[] Comics);
