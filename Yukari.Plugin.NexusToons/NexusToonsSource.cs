using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yukari.Core.Models;
using Yukari.Core.Sources;
using Yukari.Plugin.NexusToons.Data;
using Yukari.Plugin.NexusToons.Helpers;

namespace Yukari.Plugin.NexusToons;

[ComicSourceMetadata(
    "NexusToons",
    "0.1.0-Alpha+core2.4.0",
    "https://github.com/Yukari-App/Plugin.NexusToons/releases",
    "https://nexustoons.com/icon.png",
    "Acesse gratuitamente milhares de Mangás, Manhwas, Webtoons... em Português!"
)]
public class NexusToonsSource : IComicSource, IRequiresHttpClient
{
    private const int DefaultPageSize = 24;
    private const string SourceLanguage = "pt-BR";

    private static IReadOnlyList<Filter>? _filters;
    private static IReadOnlyDictionary<string, string>? _languages;

    public IReadOnlyList<Filter> Filters => _filters ??= [];

    public IReadOnlyDictionary<string, string> Languages =>
        _languages ??= new Dictionary<string, string> { { "pt-BR", "Português (BR)" } };

    private const string BaseUrl = "https://nexustoons.com/api";

    private ISharedHttpClient? _httpClient;
    private OrionCrypto _orionCrypto = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public void SetHttpClient(ISharedHttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<Comic>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        var queryParams = new Dictionary<string, string[]>
        {
            ["page"] = [page.ToString()],
            ["limit"] = [DefaultPageSize.ToString()],
            ["search"] = [query],
        };

        foreach (var kvp in filters)
            queryParams[kvp.Key] = kvp.Value.ToArray();

        string searchUrl = $"{BaseUrl}/mangas?{ToQueryString(queryParams)}";

        var searchResults = (await GetFromApiAsync<SearchResponse>(searchUrl, ct))?.Comics;

        if (searchResults is not { Length: > 0 })
            return Array.Empty<Comic>();

        var comics = searchResults
            .Select(result =>
            {
                return new Comic(
                    Id: EncodeComicId(result.Id, result.Slug),
                    ComicUrl: null,
                    Slug: null,
                    Title: result.Title,
                    Author: null,
                    Description: null,
                    Tags: [],
                    Year: null,
                    CoverImageUrl: result.CoverUrl,
                    Langs: []
                );
            })
            .ToList();

        return comics;
    }

    public async Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        var trendingFilters = new Dictionary<string, IReadOnlyList<string>>(filters)
        {
            ["sortBy"] = ["views"],
            ["sortOrder"] = ["desc"],
        };

        return await SearchAsync(string.Empty, trendingFilters, page, ct);
    }

    public async Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        var slug = DecodeComicId(comicId).Item2;
        string detailsUrl = $"{BaseUrl}/manga/{slug}";

        var detailsResult = await GetFromApiAsync<NexusToonsComic>(detailsUrl, ct);

        if (detailsResult is null)
            return null;

        return new Comic(
            Id: EncodeComicId(detailsResult.Id, detailsResult.Slug),
            ComicUrl: $"https://nexustoons.com/manga/{detailsResult.Slug}",
            Slug: null,
            Title: detailsResult.Title,
            Author: !string.IsNullOrWhiteSpace(detailsResult.Author) ? detailsResult.Author : null,
            Description: detailsResult.Description,
            Tags: detailsResult.Categories.Select(c => c.Name).ToArray(),
            Year: detailsResult.Year,
            CoverImageUrl: detailsResult.CoverUrl,
            Langs: [SourceLanguage],
            Status: GetComicStatus(detailsResult.Status)
        );
    }

    public async Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        var slug = DecodeComicId(comicId).Item2;
        string chaptersUrl = $"{BaseUrl}/manga/{slug}";

        var chaptersResult = (await GetFromApiAsync<NexusToonsComic>(chaptersUrl, ct))?.Chapters;

        if (chaptersResult is not { Length: > 0 })
            return Array.Empty<Chapter>();

        // NexusToons returns chapters in descending order (newest first).
        // Reverse to get chronological order (oldest first).
        var chapters = chaptersResult
            .Reverse()
            .Select(c => new Chapter(
                Id: c.Id.ToString(),
                Title: c.Title,
                Number: c.Number,
                Volume: null,
                Language: language,
                Groups: Array.Empty<string>(),
                LastUpdate: DateOnly.FromDateTime(c.CreatedAt.DateTime),
                Pages: null
            ))
            .ToList();

        return chapters;
    }

    public async Task<IReadOnlyList<ChapterPage>> GetChapterPagesAsync(
        string comicId,
        string chapterId,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]?> GetImageBytesAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        AddRequestHeaders(request);

        using var response = await _httpClient!.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "NexusToons Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private async Task<T?> GetFromApiAsync<T>(string url, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddRequestHeaders(request);

        using var response = await _httpClient!.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "NexusToons Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        string jsonToDeserialize = content;

        if (_orionCrypto.IsEncryptedResponse(root))
            jsonToDeserialize = _orionCrypto.ProcessResponse(doc);

        return JsonSerializer.Deserialize<T>(jsonToDeserialize, _jsonOptions);
    }

    private static void AddRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("Referer", "https://nexustoons.com");
        request.Headers.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:153.0) Gecko/20100101 Firefox/153.0"
        );
    }

    private static string EncodeComicId(int id, string slug) => $"{id}:{slug}";

    private static (string, string) DecodeComicId(string encodedId)
    {
        var splitted = encodedId.Split(":");
        return (splitted[0], splitted[1]);
    }

    private static ComicStatus GetComicStatus(string? status) =>
        status switch
        {
            "completed" => ComicStatus.Completed,
            "ongoing" => ComicStatus.Ongoing,
            "hiatus" => ComicStatus.Hiatus,
            "cancelled" => ComicStatus.Cancelled,
            _ => ComicStatus.Unknown,
        };

    private static string ToQueryString(Dictionary<string, string[]> source) =>
        string.Join(
            "&",
            source.SelectMany(kvp =>
                kvp.Value.Where(v => !string.IsNullOrEmpty(v))
                    .Select(v => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(v)}")
            )
        );
}
