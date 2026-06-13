namespace GLOWAPI.Application.Models.Agendamento;

public record ClienteAgendamentoResumo(
    string Nome,
    string? Email,
    string? Telefone,
    int TotalAgendamentos,
    DateTime? UltimoAgendamentoEm);
