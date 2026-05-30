using GLOWAPI.Application.DTOs.Horarios;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IHorarioProfissionalAutonomoService
{
    Task<IReadOnlyList<HorarioProfissionalResponseDto>> ListarAsync(
        int profissionalId,
        HorarioProfissionalAutonomoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> CriarAsync(
        int profissionalId,
        CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> AtualizarAsync(
        int profissionalId,
        int horarioId,
        AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioProfissionalResponseDto> AtualizarStatusAsync(
        int profissionalId,
        int horarioId,
        AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default);
}
