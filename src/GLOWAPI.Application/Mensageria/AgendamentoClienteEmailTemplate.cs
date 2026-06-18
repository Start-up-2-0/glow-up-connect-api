using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Mensageria;

public static class AgendamentoClienteEmailTemplate
{
    public static string Criado(Agendamento agendamento, Estabelecimento estabelecimento, Profissional profissional)
    {
        var inicio = ObterInicio(agendamento);
        return TransacionalEmailTemplate.Criar(
            titulo: "Agendamento recebido",
            preheader: $"Recebemos seu pedido de agendamento em {estabelecimento.Nome}.",
            paragrafos:
            [
                $"Recebemos seu pedido de agendamento em {estabelecimento.Nome} com {profissional.NomePublico} para {inicio:dd/MM/yyyy HH:mm}.",
                "Aguarde a confirmacao da loja."
            ]);
    }

    public static string Confirmado(Agendamento agendamento, Estabelecimento estabelecimento) =>
        CriarStatus(agendamento, estabelecimento, "Agendamento confirmado", "confirmado");

    public static string Cancelado(Agendamento agendamento, Estabelecimento estabelecimento, string motivo) =>
        CriarStatus(agendamento, estabelecimento, "Agendamento cancelado", "cancelado", $"Motivo: {motivo}");

    public static string Remarcado(Agendamento agendamento, Estabelecimento estabelecimento) =>
        CriarStatus(agendamento, estabelecimento, "Agendamento remarcado", "remarcado");

    public static string PropostaRemarcacao(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        DateOnly dataSugerida,
        TimeOnly horarioInicioSugerido,
        string motivo,
        string linkResposta)
    {
        var inicioAtual = ObterInicio(agendamento);
        return TransacionalEmailTemplate.Criar(
            titulo: "Sugestao de reagendamento",
            preheader: $"A loja {estabelecimento.Nome} sugeriu um novo horario para seu agendamento.",
            paragrafos:
            [
                $"A loja {estabelecimento.Nome} sugeriu um novo horario para seu agendamento: {dataSugerida:dd/MM/yyyy} as {horarioInicioSugerido:HH:mm}.",
                $"Horario atual: {inicioAtual:dd/MM/yyyy HH:mm}.",
                $"Motivo: {motivo}."
            ],
            botao: new EmailTemplateBotao
            {
                Texto = "Responder sugestao",
                Url = linkResposta,
                Estilo = EmailTemplateBotaoEstilo.Link
            },
            linkFallback: linkResposta);
    }

    public static string Concluido(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string linkAvaliacao)
    {
        var inicio = ObterInicio(agendamento);
        return TransacionalEmailTemplate.Criar(
            titulo: "Avalie seu atendimento",
            preheader: $"Como foi sua experiencia em {estabelecimento.Nome}?",
            paragrafos:
            [
                $"Seu atendimento em {estabelecimento.Nome} com {profissional.NomePublico} em {inicio:dd/MM/yyyy HH:mm} foi concluido.",
                "Sua opiniao ajuda outros clientes e a loja a melhorar o servico."
            ],
            botao: new EmailTemplateBotao
            {
                Texto = "Avaliar atendimento",
                Url = linkAvaliacao,
                Estilo = EmailTemplateBotaoEstilo.Link
            },
            linkFallback: linkAvaliacao);
    }

    private static string CriarStatus(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        string titulo,
        string acao,
        string? complemento = null)
    {
        var inicio = ObterInicio(agendamento);
        var servicos = ObterServicos(agendamento);
        var paragrafos = new List<string>
        {
            $"Seu agendamento em {estabelecimento.Nome} foi {acao} para {inicio:dd/MM/yyyy HH:mm}.",
            $"Servicos: {servicos}."
        };

        if (!string.IsNullOrWhiteSpace(complemento))
        {
            paragrafos.Add(complemento);
        }

        return TransacionalEmailTemplate.Criar(
            titulo: titulo,
            preheader: $"Seu agendamento em {estabelecimento.Nome} foi {acao}.",
            paragrafos: paragrafos);
    }

    private static DateTime ObterInicio(Agendamento agendamento)
    {
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        return inicio == default ? DateTime.UtcNow : inicio;
    }

    private static string ObterServicos(Agendamento agendamento) =>
        string.Join(", ", agendamento.Itens.Select(item => item.Servico?.Nome ?? "Servico"));
}
