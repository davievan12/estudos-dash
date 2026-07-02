namespace EstudosDash.Models;

/// <summary>
/// Uma matéria / tópico de estudo vindo de uma database do Notion.
/// </summary>
public record Subject(
    string Id,
    string Name,
    string? Status,
    string? Category,
    double? Progress,
    string? Url
);
