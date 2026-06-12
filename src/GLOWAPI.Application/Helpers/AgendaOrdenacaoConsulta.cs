using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Helpers;

public static class AgendaOrdenacaoConsulta
{
    public const string AtendimentoDesc = "atendimento_desc";
    public const string AtendimentoAsc = "atendimento_asc";
    public const string CriacaoDesc = "criacao_desc";
    public const string CriacaoAsc = "criacao_asc";
    public const string Padrao = AtendimentoDesc;

    public static string Normalizar(string? ordenacao) =>
        ordenacao?.Trim().ToLowerInvariant() switch
        {
            AtendimentoAsc => AtendimentoAsc,
            CriacaoDesc => CriacaoDesc,
            CriacaoAsc => CriacaoAsc,
            "proximos" => AtendimentoAsc,
            "recentes" => CriacaoDesc,
            _ => AtendimentoDesc,
        };

    public static IQueryable<Agendamento> AplicarOrdenacaoAgendamentos(
        IQueryable<Agendamento> query,
        string? ordenacao) =>
        Normalizar(ordenacao) switch
        {
            AtendimentoAsc => query.OrderBy(agendamento => agendamento.Inicio).ThenBy(agendamento => agendamento.Id),
            CriacaoDesc => query.OrderByDescending(agendamento => agendamento.CreateAd).ThenByDescending(agendamento => agendamento.Id),
            CriacaoAsc => query.OrderBy(agendamento => agendamento.CreateAd).ThenBy(agendamento => agendamento.Id),
            _ => query.OrderByDescending(agendamento => agendamento.Inicio).ThenByDescending(agendamento => agendamento.Id),
        };

    public static IQueryable<AgendamentoItem> AplicarOrdenacaoItens(
        IQueryable<AgendamentoItem> query,
        string? ordenacao) =>
        Normalizar(ordenacao) switch
        {
            AtendimentoAsc => query.OrderBy(item => item.Inicio).ThenBy(item => item.Id),
            CriacaoDesc => query.OrderByDescending(item => item.Agendamento!.CreateAd).ThenByDescending(item => item.Id),
            CriacaoAsc => query.OrderBy(item => item.Agendamento!.CreateAd).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.Inicio).ThenByDescending(item => item.Id),
        };
}
