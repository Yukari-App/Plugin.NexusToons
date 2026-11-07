using System.Net;
using Yukari.Core.Models;
using Yukari.Core.Sources;
using Yukari.Core.Sources.Http;
using Yukari.Plugin.NexusToons.Data;

namespace Yukari.Plugin.NexusToons;

[ComicSourceMetadata(
    "NexusToons",
    "1.1.0+core2.5.0",
    "https://github.com/Yukari-App/Plugin.NexusToons/releases",
    "https://nexustoons.com/icon.png",
    "Acesse gratuitamente milhares de Mangás, Manhwas, Webtoons... em Português!"
)]
public partial class NexusToonsSource : IComicSource, IRequiresHttpClient
{
    private const int DefaultPageSize = 24;
    private const string SourceLanguage = "pt-BR";
    private const string BaseUrl = "https://nexustoons.com/api";

    private ISharedHttpClient? _httpClient;

    public IReadOnlyList<Filter> Filters => _filters;
    public IReadOnlyDictionary<string, string> Languages => _languages;

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
            Title: detailsResult.Title,
            Author: !string.IsNullOrWhiteSpace(detailsResult.Author) ? detailsResult.Author : null,
            Description: detailsResult.Description,
            Tags: detailsResult.Categories?.Select(c => c.Name).ToArray() ?? [],
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
        string pagesUrl = $"{BaseUrl}/chapter/{chapterId}";

        var pagesResult = (await GetFromApiAsync<NexusToonsChapter>(pagesUrl, ct))?.Pages;

        if (pagesResult is not { Length: > 0 })
            return Array.Empty<ChapterPage>();

        var pages = new List<ChapterPage>(pagesResult.Length);
        for (int i = 0; i < pagesResult.Length; i++)
        {
            pages.Add(new ChapterPage(Number: i + 1, ImageUrl: pagesResult[i].ImageUrl));
        }

        return pages;
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
}
