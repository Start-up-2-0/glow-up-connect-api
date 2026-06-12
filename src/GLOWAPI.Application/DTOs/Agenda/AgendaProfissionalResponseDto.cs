using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Agenda;

public record AgendaProfissionalResponseDto(
    int AgendamentoItemId,
    int AgendamentoId,
    int? UsuarioClienteId,
    string ClienteNome,
    string? ClienteEmail,
    string? ClienteTelefone,
    int ServicoId,
    string ServicoNome,
    DateTime Inicio,
    DateTime Fim,
    string Status,
    string AgendamentoStatus)
{
    public static AgendaProfissionalResponseDto From(AgendamentoItem item) =>
        new(
            item.Id,
            item.AgendamentoId,
            item.Agendamento?.UsuarioClienteId,
            item.Agendamento?.UsuarioCliente?.Nome ?? item.Agendamento?.ClienteNome ?? string.Empty,
            item.Agendamento?.UsuarioCliente?.Email ?? item.Agendamento?.ClienteEmail,
            item.Agendamento?.UsuarioCliente?.Telefone ?? item.Agendamento?.ClienteTelefone,
            item.ServicoId,
            item.Servico?.Nome ?? string.Empty,
            item.Inicio,
            item.Fim,
            item.Status.ToString(),
            item.Agendamento?.Status.ToString() ?? string.Empty);
}
