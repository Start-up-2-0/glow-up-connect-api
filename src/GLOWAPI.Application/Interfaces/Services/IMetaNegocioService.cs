using GLOWAPI.Application.DTOs.Financeiro;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IMetaNegocioService
{
    Task<IReadOnlyList<MetaResponseDto>> ListarMetasAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<MetaResponseDto> CriarMetaAsync(
        int estabelecimentoId,
        CriarMetaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MetaResponseDto> AtualizarMetaAsync(
        int estabelecimentoId,
        int metaId,
        AtualizarMetaRequestDto request,
        CancellationToken cancellationToken = default);

    Task DesativarMetaAsync(
        int estabelecimentoId,
        int metaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetaProgressoProfissionalDto>> ListarProgressoAsync(
        int estabelecimentoId,
        int? metaId,
        int? mes = null,
        int? ano = null,
        CancellationToken cancellationToken = default);
}
