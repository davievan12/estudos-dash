using EstudosDash.Models;

namespace EstudosDash.Services;

/// <summary>Abstração do acesso ao Notion (facilita testes e troca de fonte).</summary>
public interface INotionClient
{
    /// <summary>Consulta a database de estudos no Notion e devolve as matérias.</summary>
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default);

    /// <summary>True quando token e database estão configurados.</summary>
    bool IsConfigured { get; }
}
