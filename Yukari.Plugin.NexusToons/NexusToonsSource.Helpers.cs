using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yukari.Core.Models;
using Yukari.Plugin.NexusToons.Helpers;

namespace Yukari.Plugin.NexusToons;

public partial class NexusToonsSource
{
    private static readonly IReadOnlyList<Filter> _filters =
    [
        new Filter(
            Key: "includeNsfw",
            DisplayName: "Conteúdo Adulto",
            Options:
            [
                new FilterOption("false", "Ocultar", true),
                new FilterOption("true", "Incluir", false),
            ],
            AllowMultiple: false
        ),
        new Filter(
            Key: "type",
            DisplayName: "Tipo",
            Options:
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
            Options:
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
            Options:
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

    private static readonly IReadOnlyDictionary<string, string> _languages = new Dictionary<
        string,
        string
    >
    {
        { "pt-BR", "Português (BR)" },
    };

    private readonly OrionCrypto _orionCrypto = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

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
