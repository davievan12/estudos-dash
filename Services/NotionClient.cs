using System.Text;
using System.Text.Json;
using EstudosDash.Models;
using Microsoft.Extensions.Options;

namespace EstudosDash.Services;

/// <summary>Configuração do Notion (lida de appsettings ou variáveis de ambiente).</summary>
public sealed class NotionOptions
{
    public string Token { get; set; } = "";
    public string DatabaseId { get; set; } = "";

    // Nomes das propriedades na SUA database — ajuste conforme o seu Notion.
    public string StatusProperty { get; set; } = "Status";
    public string CategoryProperty { get; set; } = "Matéria";
    public string ProgressProperty { get; set; } = "Progresso";
}

/// <summary>
/// Cliente da API do Notion. Consulta uma database e mapeia cada página para <see cref="Subject"/>.
/// O parsing é tolerante: acha o título por tipo e lê status/select/number por nome de propriedade.
/// </summary>
public sealed class NotionClient : INotionClient
{
    private const string ApiVersion = "2022-06-28";

    private readonly HttpClient _http;
    private readonly NotionOptions _opt;
    private readonly ILogger<NotionClient> _log;

    public NotionClient(HttpClient http, IOptions<NotionOptions> opt, ILogger<NotionClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opt.Token) && !string.IsNullOrWhiteSpace(_opt.DatabaseId);

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Notion não configurado (defina Notion:Token e Notion:DatabaseId).");

        using var req = new HttpRequestMessage(
            HttpMethod.Post, $"https://api.notion.com/v1/databases/{_opt.DatabaseId}/query");
        req.Headers.Add("Authorization", $"Bearer {_opt.Token}");
        req.Headers.Add("Notion-Version", ApiVersion);
        req.Content = new StringContent("{\"page_size\":100}", Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Notion respondeu {Status}: {Body}", (int)resp.StatusCode, body);
            throw new HttpRequestException($"Notion API retornou {(int)resp.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);
        var results = doc.RootElement.GetProperty("results");

        var list = new List<Subject>(results.GetArrayLength());
        foreach (var page in results.EnumerateArray())
            list.Add(MapPage(page));
        return list;
    }

    private Subject MapPage(JsonElement page)
    {
        var id = page.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        var url = page.TryGetProperty("url", out var u) ? u.GetString() : null;
        var props = page.GetProperty("properties");

        return new Subject(
            Id: id,
            Name: ExtractTitle(props) ?? "(sem título)",
            Status: ExtractSelectLike(props, _opt.StatusProperty),
            Category: ExtractSelectLike(props, _opt.CategoryProperty),
            Progress: ExtractNumber(props, _opt.ProgressProperty),
            Url: url
        );
    }

    /// <summary>Acha a propriedade do tipo "title" (independente do nome) e concatena o texto.</summary>
    private static string? ExtractTitle(JsonElement props)
    {
        foreach (var p in props.EnumerateObject())
        {
            if (p.Value.TryGetProperty("type", out var t) && t.GetString() == "title")
            {
                var sb = new StringBuilder();
                foreach (var rt in p.Value.GetProperty("title").EnumerateArray())
                    if (rt.TryGetProperty("plain_text", out var pt)) sb.Append(pt.GetString());
                return sb.Length > 0 ? sb.ToString() : null;
            }
        }
        return null;
    }

    /// <summary>Lê uma propriedade "status" ou "select" pelo nome, devolvendo o nome do valor.</summary>
    private static string? ExtractSelectLike(JsonElement props, string propName)
    {
        if (!props.TryGetProperty(propName, out var prop)) return null;
        var type = prop.TryGetProperty("type", out var t) ? t.GetString() : null;

        if ((type == "status" || type == "select") &&
            prop.TryGetProperty(type, out var inner) &&
            inner.ValueKind == JsonValueKind.Object &&
            inner.TryGetProperty("name", out var name))
        {
            return name.GetString();
        }
        return null;
    }

    private static double? ExtractNumber(JsonElement props, string propName)
    {
        if (props.TryGetProperty(propName, out var prop)
            && prop.TryGetProperty("number", out var num)
            && num.ValueKind == JsonValueKind.Number)
            return num.GetDouble();
        return null;
    }
}
