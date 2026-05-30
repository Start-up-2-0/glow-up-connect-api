using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Agenda;

public record AgendaGeralFiltro(
    int EstabelecimentoId,
    int? ProfissionalId,
    int? ClienteId,
    AgendamentoStatus? Status,
    DateTime? Inicio,
    DateTime? Fim);
