using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record NexusToonsChapter(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
);
