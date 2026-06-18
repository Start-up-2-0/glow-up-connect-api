using GLOWAPI.Application.Helpers;
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
    string Ordenacao)
{
    public static AgendamentoClienteFiltro Criar(
        int usuarioClienteId,
        AgendamentoStatus? status,
        DateTime? dataInicio,
        DateTime? dataFim,
        int? estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        string? ordenacao) =>
        new(
            usuarioClienteId,
            status,
            dataInicio,
            dataFim,
            estabelecimentoId,
            pagina,
            tamanhoPagina,
            AgendaOrdenacaoConsulta.Normalizar(ordenacao));
}
