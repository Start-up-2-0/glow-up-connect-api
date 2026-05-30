using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Agenda;

public record AgendaGeralResponseDto(
    int Id,
    int UsuarioClienteId,
    string ClienteNome,
    string Status,
    decimal ValorTotal,
    string Observacao,
    IReadOnlyList<AgendaGeralItemResponseDto> Itens)
{
    public static AgendaGeralResponseDto From(Agendamento agendamento) =>
        new(
            agendamento.Id,
            agendamento.UsuarioClienteId,
            agendamento.UsuarioCliente?.Nome ?? string.Empty,
            agendamento.Status.ToString(),
            agendamento.ValorTotal,
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
