using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yukari.Core.Models;
using Yukari.Core.Sources;
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
    private static IReadOnlyList<Filter>? _filters;
    private static IReadOnlyDictionary<string, string>? _languages;

    public IReadOnlyList<Filter> Filters => _filters ??= [];

    public IReadOnlyDictionary<string, string> Languages =>
        _languages ??= new Dictionary<string, string> { { "pt-br", "Português (BR)" } };

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
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
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
}
