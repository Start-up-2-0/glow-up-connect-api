using GLOWAPI.Application.DTOs.Agenda;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAtendimentoProfissionalService
{
    Task<AtendimentoProfissionalResponseDto> IniciarAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default);

    Task<AtendimentoProfissionalResponseDto> FinalizarAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default);
}
