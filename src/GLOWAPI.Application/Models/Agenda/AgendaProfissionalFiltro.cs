using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Agenda;

public record AgendaProfissionalFiltro(
    int EstabelecimentoId,
    int ProfissionalId,
    AgendamentoItemStatus? Status,
    DateTime? Inicio,
    DateTime? Fim);
