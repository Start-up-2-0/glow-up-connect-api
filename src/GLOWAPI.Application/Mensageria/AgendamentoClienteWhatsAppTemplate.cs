using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Mensageria;

public static class AgendamentoClienteWhatsAppTemplate
{
    public static string Confirmado(Agendamento agendamento, Estabelecimento estabelecimento)
    {
        var inicio = ObterInicio(agendamento);
        var servicos = ObterServicos(agendamento);
        return $"Seu agendamento em {estabelecimento.Nome} foi confirmado para {inicio:dd/MM/yyyy HH:mm}. Servicos: {servicos}.";
    }

    public static string Cancelado(Agendamento agendamento, Estabelecimento estabelecimento, string motivo)
    {
        var inicio = ObterInicio(agendamento);
        return $"Seu agendamento em {estabelecimento.Nome} de {inicio:dd/MM/yyyy HH:mm} foi cancelado. Motivo: {motivo}.";
    }

    public static string Remarcado(Agendamento agendamento, Estabelecimento estabelecimento)
    {
        var inicio = ObterInicio(agendamento);
        var servicos = ObterServicos(agendamento);
        return $"Seu agendamento em {estabelecimento.Nome} foi remarcado para {inicio:dd/MM/yyyy HH:mm}. Servicos: {servicos}.";
    }

    private static DateTime ObterInicio(Agendamento agendamento)
    {
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        return inicio == default ? DateTime.UtcNow : inicio;
    }

    private static string ObterServicos(Agendamento agendamento) =>
        string.Join(", ", agendamento.Itens.Select(item => item.Servico?.Nome ?? "Servico"));
}
