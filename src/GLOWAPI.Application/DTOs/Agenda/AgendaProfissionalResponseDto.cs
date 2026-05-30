using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Agenda;

public record AgendaProfissionalResponseDto(
    int AgendamentoItemId,
    int AgendamentoId,
    int UsuarioClienteId,
    string ClienteNome,
    int ServicoId,
    string ServicoNome,
    DateTime Inicio,
    DateTime Fim,
    string Status)
{
    public static AgendaProfissionalResponseDto From(AgendamentoItem item) =>
        new(
            item.Id,
            item.AgendamentoId,
            item.Agendamento?.UsuarioClienteId ?? 0,
            item.Agendamento?.UsuarioCliente?.Nome ?? string.Empty,
            item.ServicoId,
            item.Servico?.Nome ?? string.Empty,
            item.Inicio,
            item.Fim,
            item.Status.ToString());
}
