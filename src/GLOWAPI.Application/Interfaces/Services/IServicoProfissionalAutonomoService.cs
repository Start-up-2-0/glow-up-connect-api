using GLOWAPI.Application.DTOs.Servicos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IServicoProfissionalAutonomoService
{
    Task<IReadOnlyList<ServicoResponseDto>> ListarAsync(
        int profissionalId,
        ServicoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> CriarAsync(
        int profissionalId,
        CriarServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> AtualizarAsync(
        int profissionalId,
        int servicoId,
        AtualizarServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServicoResponseDto> AtualizarStatusAsync(
        int profissionalId,
        int servicoId,
        AtualizarStatusServicoRequestDto request,
        CancellationToken cancellationToken = default);
}
