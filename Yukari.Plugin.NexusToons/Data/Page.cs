using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record Page([property: JsonPropertyName("imageUrl")] string ImageUrl);
