using GLOWAPI.Application.DTOs.Horarios;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IHorarioProfissionalNegocioService
{
    Task<IReadOnlyList<HorarioProfissionalResponseDto>> ListarAsync(
        int estabelecimentoId,
        HorarioProfissionalFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> CriarAsync(
        int estabelecimentoId,
        int profissionalId,
        CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);
}
