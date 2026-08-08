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
    "1.0.0+core2.4.0",
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

    public IReadOnlyList<Filter> Filters =>
        _filters ??= [
            new Filter(
                Key: "includeNsfw",
                DisplayName: "Conteúdo Adulto",
                [
                    new FilterOption("false", "Ocultar", true),
                    new FilterOption("true", "Incluir", false),
                ],
                AllowMultiple: false
            ),
            new Filter(
                Key: "type",
                DisplayName: "Tipo",
                [
                    new FilterOption("manga", "Manga", false),
                    new FilterOption("manhwa", "Manhwa", false),
                    new FilterOption("manhua", "Manhua", false),
                    new FilterOption("webtoon", "Webtoon", false),
                    new FilterOption("pornhwa", "Pornhwa", false),
                    new FilterOption("hentai", "Hentai", false),
                    new FilterOption("doujinshi", "Doujinshi", false),
                    new FilterOption("novel", "Novel", false),
                    new FilterOption("one-shot", "One-Shot", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "status",
                DisplayName: "Status",
                [
                    new FilterOption("ongoing", "Em Andamento", false),
                    new FilterOption("completed", "Completo", false),
                    new FilterOption("hiatus", "Hiato", false),
                    new FilterOption("cancelled", "Cancelado", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "genres",
                DisplayName: "Gêneros",
                [
                    new FilterOption("56", "Acadêmica", false),
                    new FilterOption("288", "Adaptação", false),
                    new FilterOption("145", "Adaptação de Novel", false),
                    new FilterOption("79", "App", false),
                    new FilterOption("7", "Aventura", false),
                    new FilterOption("2", "Ação", false),
                    new FilterOption("85", "Cartoon", false),
                    new FilterOption("34", "Club", false),
                    new FilterOption("66", "Comida", false),
                    new FilterOption("11", "Comédia", false),
                    new FilterOption("89", "Crime", false),
                    new FilterOption("43", "Culinária", false),
                    new FilterOption("194", "Curta", false),
                    new FilterOption("41", "Cyberpunk", false),
                    new FilterOption("191", "Doujinshi", false),
                    new FilterOption("12", "Drama", false),
                    new FilterOption("24", "Esportes", false),
                    new FilterOption("116", "Estratégia", false),
                    new FilterOption("144", "Estudante", false),
                    new FilterOption("275", "Familia", false),
                    new FilterOption("69", "Família", false),
                    new FilterOption("8", "Fantasia", false),
                    new FilterOption("332", "Ficção", false),
                    new FilterOption("16", "Ficção Científica", false),
                    new FilterOption("235", "Game", false),
                    new FilterOption("46", "Guerra", false),
                    new FilterOption("132", "Gênio", false),
                    new FilterOption("17", "Histórico", false),
                    new FilterOption("6", "Josei", false),
                    new FilterOption("70", "Magia", false),
                    new FilterOption("39", "Mecha", false),
                    new FilterOption("50", "Metaverso", false),
                    new FilterOption("45", "Militar", false),
                    new FilterOption("13", "Mistério", false),
                    new FilterOption("58", "Mitologia", false),
                    new FilterOption("148", "Musical", false),
                    new FilterOption("98", "Médico", false),
                    new FilterOption("42", "Música", false),
                    new FilterOption("278", "Nerd", false),
                    new FilterOption("170", "Oneshot", false),
                    new FilterOption("160", "Policial", false),
                    new FilterOption("129", "Polícia", false),
                    new FilterOption("63", "Psicológico", false),
                    new FilterOption("217", "Psicopata", false),
                    new FilterOption("183", "Realismo", false),
                    new FilterOption("10", "Romance", false),
                    new FilterOption("336", "Samurai", false),
                    new FilterOption("296", "Sangue", false),
                    new FilterOption("5", "Seinen", false),
                    new FilterOption("4", "Shoujo", false),
                    new FilterOption("3", "Shounen", false),
                    new FilterOption("161", "Sombrio", false),
                    new FilterOption("105", "Supernatural", false),
                    new FilterOption("14", "Suspense", false),
                    new FilterOption("159", "Tecnológico", false),
                    new FilterOption("136", "Terror", false),
                    new FilterOption("338", "Traduzida por IA", false),
                    new FilterOption("86", "Tragédia", false),
                    new FilterOption("147", "Violência", false),
                    new FilterOption("240", "Webcomic", false),
                    new FilterOption("77", "campus", false),
                    new FilterOption("107", "fantasia", false),
                    new FilterOption("111", "luta", false),
                    new FilterOption("108", "sistema", false),
                    new FilterOption("60", "sobrenatural", false),
                    new FilterOption("110", "sobrevivencia", false),
                    new FilterOption("109", "torre", false),
                    new FilterOption("298", "vídeo game", false),
                ],
                AllowMultiple: true
            ),
        ];

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

    private static string EncodeComicId(int id, string slug) => $"{id}%{slug}";

    private static (string, string) DecodeComicId(string encodedId)
    {
        var splitted = encodedId.Split("%");
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
            source.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(string.Join(",", kvp.Value.Where(v => !string.IsNullOrEmpty(v))))}"
            )
        );
}
