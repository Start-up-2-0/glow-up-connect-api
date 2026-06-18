using GLOWAPI.Domain.Entities;
using AgendamentoEntity = GLOWAPI.Domain.Entities.Agendamento;

namespace GLOWAPI.Application.DTOs.Agenda;

public record AgendaGeralResponseDto(
    int Id,
    int? UsuarioClienteId,
    string ClienteNome,
    string? ClienteEmail,
    string? ClienteTelefone,
    string Status,
    decimal ValorTotal,
    DateTime Inicio,
    DateTime Fim,
    string Observacao,
    IReadOnlyList<AgendaGeralItemResponseDto> Itens)
{
    public static AgendaGeralResponseDto From(AgendamentoEntity agendamento) =>
        new(
            agendamento.Id,
            agendamento.UsuarioClienteId,
            agendamento.UsuarioCliente?.Nome ?? agendamento.ClienteNome ?? string.Empty,
            agendamento.UsuarioCliente?.Email ?? agendamento.ClienteEmail,
            agendamento.UsuarioCliente?.Telefone ?? agendamento.ClienteTelefone,
            agendamento.Status.ToString(),
            agendamento.ValorTotal,
            agendamento.Inicio,
            agendamento.Fim,
            agendamento.Observacao,
            agendamento.Itens
                .OrderBy(item => item.Inicio)
                .Select(AgendaGeralItemResponseDto.From)
                .ToList());
}

public record AgendaGeralItemResponseDto(
    int Id,
    int ServicoId,
    string ServicoNome,
    int ProfissionalId,
    string ProfissionalNome,
    DateTime Inicio,
    DateTime Fim,
    decimal Valor,
    string Status)
{
    public static AgendaGeralItemResponseDto From(AgendamentoItem item) =>
        new(
            item.Id,
            item.ServicoId,
            item.Servico?.Nome ?? string.Empty,
            item.ProfissionalId,
            item.Profissional?.NomePublico ?? string.Empty,
            item.Inicio,
            item.Fim,
            item.Valor,
            item.Status.ToString());
}
