using GLOWAPI.Application.DTOs.Estabelecimentos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IComodidadePerfilService
{
    Task<IReadOnlyList<ComodidadeDto>> ListarCatalogoAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComodidadeDto>> ListarPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComodidadeDto>> AtualizarEstabelecimentoAsync(int estabelecimentoId, IReadOnlyCollection<int> comodidadeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComodidadeDto>> ListarPorProfissionalAutonomoAsync(int profissionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComodidadeDto>> AtualizarProfissionalAutonomoAsync(int profissionalId, IReadOnlyCollection<int> comodidadeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComodidadeDto>> ListarPublicasAsync(Guid estabelecimentoPublicGuid, CancellationToken cancellationToken = default);
}
