using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record Category(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] CategoryAttr CategoryAttribute
);
