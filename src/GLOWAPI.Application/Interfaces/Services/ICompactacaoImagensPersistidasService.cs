using GLOWAPI.Application.Models.Manutencao;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICompactacaoImagensPersistidasService
{
    Task<CompactacaoImagensResultado> ProcessarLoteAsync(
        CompactacaoImagensCursor cursor,
        int tamanhoLote,
        CancellationToken cancellationToken = default);
}

public sealed class CompactacaoImagensCursor
{
    public int Usuarios { get; set; }
    public int Estabelecimentos { get; set; }
    public int Profissionais { get; set; }
}
