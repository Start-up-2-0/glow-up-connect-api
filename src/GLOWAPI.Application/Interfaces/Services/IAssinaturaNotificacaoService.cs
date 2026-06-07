using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaNotificacaoService
{
    Task AssinaturaIniciadaAsync(
        Assinatura assinatura,
        Plano plano,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task PagamentoConfirmadoAsync(
        Assinatura assinatura,
        Pagamento pagamento,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task PagamentoRecusadoAsync(
        Pagamento pagamento,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task AssinaturaCanceladaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task AssinaturaSuspensaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task AlertaFaturaProximaAsync(
        Assinatura assinatura,
        string? destinatario,
        CancellationToken cancellationToken = default);

    Task TrialIniciadoAsync(
        Assinatura assinatura,
        Plano plano,
        int diasTrial,
        string? destinatario,
        CancellationToken cancellationToken = default);
}
