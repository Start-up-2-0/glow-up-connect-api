using GLOWAPI.Application.DTOs.Horarios;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IHorarioFuncionamentoNegocioService
{
    Task<IReadOnlyList<HorarioFuncionamentoResponseDto>> ListarAsync(
        int estabelecimentoId,
        HorarioFuncionamentoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<HorarioFuncionamentoResponseDto> CriarAsync(
        int estabelecimentoId,
        CriarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioFuncionamentoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HorarioFuncionamentoResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarStatusHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default);
}
