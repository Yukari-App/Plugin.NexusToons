using System.Net;
using Yukari.Core.Models;
using Yukari.Core.Sources;

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
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
