using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Mensageria;

public static class AgendamentoClienteWhatsAppTemplate
{
    private const string Footer = "\n\n───\n💡 *Glow Up Connect* — Seu jeito inteligente de agendar.";

    public static string Confirmado(Agendamento agendamento, Estabelecimento estabelecimento)
    {
        var inicio = ObterInicio(agendamento);
        var servicos = ObterServicos(agendamento);
        return $"✅ *Agendamento Confirmado!* 🎉\n\n"
            + $"Seu agendamento em *{estabelecimento.Nome}* foi confirmado.\n\n"
            + $"📅 *Data:* {inicio:dd/MM/yyyy}\n"
            + $"⏰ *Horário:* {inicio:HH:mm}\n"
            + $"💇 *Serviços:* {servicos}"
            + Footer;
    }

    public static string Cancelado(Agendamento agendamento, Estabelecimento estabelecimento, string motivo)
    {
        var inicio = ObterInicio(agendamento);
        return $"❌ *Agendamento Cancelado*\n\n"
            + $"Seu agendamento em *{estabelecimento.Nome}* de *{inicio:dd/MM/yyyy HH:mm}* foi cancelado.\n\n"
            + $"📝 *Motivo:* {motivo}\n\n"
            + $"Precisa de ajuda? Entre em contato com o estabelecimento."
            + Footer;
    }

    public static string Remarcado(Agendamento agendamento, Estabelecimento estabelecimento)
    {
        var inicio = ObterInicio(agendamento);
        var servicos = ObterServicos(agendamento);
        return $"🔁 *Agendamento Remarcado*\n\n"
            + $"Seu agendamento em *{estabelecimento.Nome}* foi remarcado.\n\n"
            + $"📅 *Data:* {inicio:dd/MM/yyyy}\n"
            + $"⏰ *Horário:* {inicio:HH:mm}\n"
            + $"💇 *Serviços:* {servicos}"
            + Footer;
    }

    private static DateTime ObterInicio(Agendamento agendamento)
    {
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        return inicio == default ? DateTime.UtcNow : inicio;
    }

    private static string ObterServicos(Agendamento agendamento) =>
        string.Join(", ", agendamento.Itens.Select(item => item.Servico?.Nome ?? "Servico"));
}
