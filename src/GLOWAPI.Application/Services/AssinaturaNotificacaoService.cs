using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
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
        EnfileirarAsync(
            assinatura,
            destinatario,
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
        EnfileirarAsync(
            assinatura,
            destinatario,
            "Pagamento confirmado",
            $"Recebemos o pagamento da sua assinatura no valor de {pagamento.Valor:C}. Seus modulos ja estao liberados conforme o plano contratado.",
            "pagamento-confirmado",
            prioridade: 2,
            cancellationToken);

    public Task PagamentoRecusadoAsync(
        Pagamento pagamento,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            pagamento.Assinatura,
            destinatario,
            "Pagamento nao aprovado",
            $"Nao conseguimos confirmar o pagamento da sua assinatura no valor de {pagamento.Valor:C}. Verifique o checkout ou tente novamente.",
            "pagamento-recusado",
            prioridade: 2,
            cancellationToken);

    public Task AssinaturaCanceladaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            assinatura,
            destinatario,
            "Assinatura cancelada",
            "Sua assinatura foi cancelada. Os modulos pagos ficam bloqueados a partir do cancelamento.",
            "assinatura-cancelada",
            prioridade: 2,
            cancellationToken);

    public Task AssinaturaSuspensaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            assinatura,
            destinatario,
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
        return EnfileirarAsync(
            assinatura,
            destinatario,
            "Fatura proxima",
            $"Sua proxima cobranca de assinatura vence em {vencimento}. Verifique se o cartao esta atualizado.",
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
        EnfileirarAsync(
            assinatura,
            destinatario,
            "Trial iniciado",
            $"Voce ganhou {diasTrial} dias gratis no plano {plano.Nome}. Aproveite todos os modulos durante o periodo de teste.",
            "trial-iniciado",
            prioridade: 2,
            cancellationToken);

    private async Task EnfileirarAsync(
        Assinatura? assinatura,
        string? destinatario,
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
            Conteudo = conteudo,
            EstabelecimentoId = assinatura?.EstabelecimentoId,
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
