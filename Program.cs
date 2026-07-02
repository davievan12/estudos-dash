using EstudosDash.Services;

var builder = WebApplication.CreateBuilder(args);

// Segredos locais (gitignored). Variáveis de ambiente Notion__Token / Notion__DatabaseId
// também funcionam (já carregadas por padrão).
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Config do Notion: appsettings.json / appsettings.Local.json / variáveis de ambiente
// (ex.: Notion__Token e Notion__DatabaseId). O token NUNCA fica no repositório.
builder.Services.Configure<NotionOptions>(builder.Configuration.GetSection("Notion"));

// HttpClient tipado para o Notion, com timeout.
builder.Services.AddHttpClient<INotionClient, NotionClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
});

var app = builder.Build();

app.UseDefaultFiles(); // serve wwwroot/index.html na raiz
app.UseStaticFiles();

// Saúde da API + se o Notion está configurado.
app.MapGet("/api/health", (INotionClient notion) =>
    Results.Ok(new { status = "ok", notionConfigured = notion.IsConfigured }));

// Matérias vindas do Notion.
app.MapGet("/api/subjects", async (INotionClient notion, CancellationToken ct) =>
{
    if (!notion.IsConfigured)
    {
        return Results.Ok(new
        {
            configured = false,
            message = "Configure Notion:Token e Notion:DatabaseId (veja o README).",
            subjects = Array.Empty<object>()
        });
    }

    try
    {
        var subjects = await notion.GetSubjectsAsync(ct);
        return Results.Ok(new { configured = true, subjects });
    }
    catch (Exception ex)
    {
        return Results.Json(new { configured = true, error = ex.Message }, statusCode: 502);
    }
});

app.Run();
