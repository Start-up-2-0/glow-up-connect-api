using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAgendamentoNotificacaoService
{
    Task AgendamentoCriadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default);

    Task AgendamentoConfirmadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default);

    Task AgendamentoCanceladoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string motivo,
        CancellationToken cancellationToken = default);

    Task AgendamentoRemarcadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string? motivo,
        CancellationToken cancellationToken = default);
}
