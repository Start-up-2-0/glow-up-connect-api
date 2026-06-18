using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Agenda;

public record AgendaProfissionalFiltro(
    int EstabelecimentoId,
    int ProfissionalId,
    AgendamentoStatus? Status,
    DateTime? Inicio,
    DateTime? Fim,
    int Pagina,
    int TamanhoPagina,
    string Ordenacao);
