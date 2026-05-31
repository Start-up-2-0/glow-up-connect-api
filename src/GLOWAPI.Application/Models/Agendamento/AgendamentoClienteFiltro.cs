using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Agendamento;

public record AgendamentoClienteFiltro(
    int UsuarioClienteId,
    AgendamentoStatus? Status,
    DateTime? DataInicio,
    DateTime? DataFim,
    int? EstabelecimentoId,
    int Pagina,
    int TamanhoPagina,
    bool OrdenarPorProximos);
