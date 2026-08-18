using GLOWAPI.Application.DTOs.Agendamento;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IRecebimentoAgendamentoService
{
    Task<ReceberAgendamentoResponseDto> ReceberPresencialAsync(
        int estabelecimentoId,
        int agendamentoId,
        ReceberAgendamentoRequestDto request,
        CancellationToken cancellationToken = default);
}
