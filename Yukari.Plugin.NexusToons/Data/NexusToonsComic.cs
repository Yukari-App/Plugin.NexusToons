using System.Text.Json.Serialization;

namespace Yukari.Plugin.NexusToons.Data;

internal record NexusToonsComic(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("categories")] Category[] Categories,
    [property: JsonPropertyName("releaseYear")] int? Year,
    [property: JsonPropertyName("coverImage")] string CoverUrl,
    [property: JsonPropertyName("status")] string Status
);
