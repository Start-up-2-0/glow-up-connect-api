using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AssinaturaNotificacaoService : IAssinaturaNotificacaoService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public AssinaturaNotificacaoService(IMensagemNotificacaoService mensagemNotificacaoService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
    }

    public Task AssinaturaIniciadaAsync(
        Assinatura assinatura,
        Plano plano,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura?.EstabelecimentoId,
            "Assinatura iniciada",
            $"Sua assinatura do plano {plano.Nome} foi iniciada e aguarda confirmacao de pagamento.",
            "assinatura-iniciada",
            prioridade: 2,
            cancellationToken);

    public Task PagamentoConfirmadoAsync(
        Assinatura assinatura,
        Pagamento pagamento,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura.EstabelecimentoId,
            "Pagamento confirmado",
            $"Recebemos o pagamento da sua assinatura no valor de {pagamento.Valor:C}. Seus modulos ja estao liberados conforme o plano contratado.",
            "pagamento-confirmado",
            prioridade: 2,
            cancellationToken);

    public Task PagamentoRecusadoAsync(
        Pagamento pagamento,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            pagamento.Assinatura,
            destinatario,
            pagamento.Assinatura?.EstabelecimentoId,
            "Pagamento nao aprovado",
            $"Nao conseguimos confirmar o pagamento da sua assinatura no valor de {pagamento.Valor:C}. Verifique o checkout ou tente novamente.",
            "pagamento-recusado",
            prioridade: 2,
            cancellationToken);

    public Task AssinaturaCanceladaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura.EstabelecimentoId,
            "Assinatura cancelada",
            "Sua assinatura foi cancelada. Os modulos pagos ficam bloqueados a partir do cancelamento.",
            "assinatura-cancelada",
            prioridade: 2,
            cancellationToken);

    public Task AssinaturaSuspensaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura.EstabelecimentoId,
            "Assinatura suspensa",
            "Sua assinatura foi suspensa. Regularize a situacao para recuperar o acesso aos modulos.",
            "assinatura-suspensa",
            prioridade: 2,
            cancellationToken);

    public Task AlertaFaturaProximaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default)
    {
        var vencimento = assinatura.ProximaDataVencimento?.ToString("dd/MM/yyyy") ?? "em breve";
        return EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura.EstabelecimentoId,
            "Fatura proxima",
            $"Sua proxima cobranca de assinatura vence em {vencimento}. Em breve enviaremos o link de pagamento pelo e-mail e WhatsApp.",
            "alerta-fatura-proxima",
            prioridade: 2,
            cancellationToken);
    }

    public Task TrialIniciadoAsync(
        Assinatura assinatura,
        Plano plano,
        int diasTrial,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarEmailAsync(
            assinatura,
            destinatario,
            assinatura.EstabelecimentoId,
            "Trial iniciado",
            $"Voce ganhou {diasTrial} dias gratis no plano {plano.Nome}. Aproveite todos os modulos durante o periodo de teste.",
            "trial-iniciado",
            prioridade: 2,
            cancellationToken);

    public async Task CobrancaPendenteComLinkAsync(
        Assinatura assinatura,
        Pagamento pagamento,
        string checkoutUrl,
        AssinaturaTitularContato titular,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            return;
        }

        var vencimento = pagamento.DataVencimento?.ToString("dd/MM/yyyy") ?? "em breve";
        var assunto = "Link de pagamento da assinatura";
        var conteudo =
            $"Sua cobranca de assinatura no valor de {pagamento.Valor:C} com vencimento em {vencimento} esta disponivel.\n\n" +
            $"Pague pelo Mercado Pago: {checkoutUrl.Trim()}";

        var payload = JsonSerializer.Serialize(new
        {
            evento = "cobranca-pendente-link",
            assinaturaId = assinatura.Id,
            pagamentoId = pagamento.Id,
            valor = pagamento.Valor,
            vencimento = pagamento.DataVencimento,
            checkoutUrl = checkoutUrl.Trim()
        });

        var conteudoEmail = TransacionalEmailTemplate.Criar(
            assunto,
            assunto,
            [
                $"Sua cobranca de assinatura no valor de {pagamento.Valor:C} com vencimento em {vencimento} esta disponivel.",
                "Pague pelo Mercado Pago usando o botao abaixo."
            ],
            botao: new EmailTemplateBotao
            {
                Texto = "Pagar assinatura",
                Url = checkoutUrl.Trim(),
                Estilo = EmailTemplateBotaoEstilo.Link
            },
            linkFallback: checkoutUrl.Trim());

        if (!string.IsNullOrWhiteSpace(titular.Email))
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = titular.Email.Trim(),
                Assunto = assunto,
                Conteudo = conteudoEmail,
                EstabelecimentoId = titular.EstabelecimentoId ?? assinatura.EstabelecimentoId,
                Prioridade = 1,
                PayloadJson = payload
            }, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(titular.TelefoneWhatsApp))
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.WhatsApp,
                Destinatario = titular.TelefoneWhatsApp.Trim(),
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = titular.EstabelecimentoId ?? assinatura.EstabelecimentoId,
                Prioridade = 1,
                PayloadJson = payload
            }, cancellationToken);
        }
    }

    private async Task EnfileirarEmailAsync(
        Assinatura? assinatura,
        string? destinatario,
        int? estabelecimentoId,
        string assunto,
        string conteudo,
        string evento,
        int prioridade,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return;
        }

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = destinatario.Trim(),
            Assunto = assunto,
            Conteudo = TransacionalEmailTemplate.Criar(assunto, assunto, [conteudo]),
            EstabelecimentoId = estabelecimentoId ?? assinatura?.EstabelecimentoId,
            Prioridade = prioridade,
            PayloadJson = JsonSerializer.Serialize(new
            {
                evento,
                assinaturaId = assinatura?.Id,
                planoId = assinatura?.PlanoId,
                status = assinatura?.Status.ToString()
            })
        }, cancellationToken);
    }
}
