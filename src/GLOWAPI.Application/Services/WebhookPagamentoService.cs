using System.Text.Json;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Pagamentos;

namespace GLOWAPI.Application.Services;

public class WebhookPagamentoService : IWebhookPagamentoService
{
    private readonly IWebhookPagamentoRepository _webhookPagamentoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;

    public WebhookPagamentoService(
        IWebhookPagamentoRepository webhookPagamentoRepository,
        IPagamentoRepository pagamentoRepository,
        IAssinaturaRepository assinaturaRepository)
    {
        _webhookPagamentoRepository = webhookPagamentoRepository;
        _pagamentoRepository = pagamentoRepository;
        _assinaturaRepository = assinaturaRepository;
    }

    public async Task<WebhookPagamentoResponseDto> RegistrarAsync(
        RegistrarWebhookPagamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Validar(request);

        var eventId = request.EventId.Trim();
        var webhookExistente = await _webhookPagamentoRepository.ObterPorEventoAsync(
            request.Gateway,
            eventId,
            cancellationToken);

        if (webhookExistente is not null)
        {
            return WebhookPagamentoResponseDto.From(webhookExistente, duplicado: true);
        }

        var webhook = new WebhookPagamento
        {
            Gateway = request.Gateway,
            EventId = eventId,
            EventType = request.EventType.Trim(),
            Payload = request.Payload.Trim(),
            Processado = false
        };

        await _webhookPagamentoRepository.AdicionarAsync(webhook, cancellationToken);
        await ProcessarAsync(webhook, cancellationToken);
        await _webhookPagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return WebhookPagamentoResponseDto.From(webhook, duplicado: false);
    }

    private static void Validar(RegistrarWebhookPagamentoRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            throw new WebhookPagamentoInvalidoException("EventId do webhook de pagamento e obrigatorio.");
        }

        if (request.EventId.Length > 150)
        {
            throw new WebhookPagamentoInvalidoException("EventId do webhook de pagamento deve ter no maximo 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            throw new WebhookPagamentoInvalidoException("EventType do webhook de pagamento e obrigatorio.");
        }

        if (request.EventType.Length > 120)
        {
            throw new WebhookPagamentoInvalidoException("EventType do webhook de pagamento deve ter no maximo 120 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            throw new WebhookPagamentoInvalidoException("Payload do webhook de pagamento e obrigatorio.");
        }
    }

    private async Task ProcessarAsync(WebhookPagamento webhook, CancellationToken cancellationToken)
    {
        if (EventoAssinaturaCancelada(webhook.EventType))
        {
            await ProcessarCancelamentoOuSuspensaoAsync(
                webhook,
                AssinaturaStatus.Cancelada,
                registrarCanceladoEm: true,
                cancellationToken);
            return;
        }

        if (EventoAssinaturaSuspensa(webhook.EventType))
        {
            await ProcessarCancelamentoOuSuspensaoAsync(
                webhook,
                AssinaturaStatus.Suspensa,
                registrarCanceladoEm: false,
                cancellationToken);
            return;
        }

        if (!EventoPagamentoAprovado(webhook.EventType))
        {
            return;
        }

        var gatewayPaymentId = ExtrairGatewayPaymentId(webhook.Payload);
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            webhook.ErroProcessamento = "Payload nao contem gatewayPaymentId.";
            return;
        }

        var pagamento = await _pagamentoRepository.ObterPorGatewayPaymentIdAsync(
            webhook.Gateway,
            gatewayPaymentId,
            cancellationToken);

        if (pagamento is null)
        {
            webhook.ErroProcessamento = "Pagamento nao encontrado para o gatewayPaymentId informado.";
            return;
        }

        if (pagamento.Status == PagamentoStatus.Pago)
        {
            webhook.Processado = true;
            webhook.ProcessadoEm ??= DateTime.UtcNow;
            return;
        }

        pagamento.Status = PagamentoStatus.Pago;
        pagamento.PagoEm = DateTime.UtcNow;
        pagamento.UpdatedAt = DateTime.UtcNow;
        _pagamentoRepository.Atualizar(pagamento);

        if (pagamento.Assinatura is not null)
        {
            if (pagamento.Assinatura.PlanoAlteracaoPendenteId.HasValue)
            {
                pagamento.Assinatura.PlanoId = pagamento.Assinatura.PlanoAlteracaoPendenteId.Value;
                pagamento.Assinatura.Plano = pagamento.Assinatura.PlanoAlteracaoPendente;
                pagamento.Assinatura.PlanoAlteracaoPendenteId = null;
                pagamento.Assinatura.PlanoAlteracaoPendente = null;
            }

            pagamento.Assinatura.Status = AssinaturaStatus.Ativa;
            pagamento.Assinatura.Inicio = pagamento.PagoEm.Value;
            pagamento.Assinatura.Fim = CalcularFimAssinatura(pagamento.PagoEm.Value, pagamento.Assinatura.Plano?.Periodo);
            pagamento.Assinatura.UltimoPagamentoId = pagamento.Id;
            pagamento.Assinatura.UpdatedAt = DateTime.UtcNow;
        }

        webhook.Processado = true;
        webhook.ProcessadoEm = DateTime.UtcNow;
    }

    private async Task ProcessarCancelamentoOuSuspensaoAsync(
        WebhookPagamento webhook,
        AssinaturaStatus novoStatus,
        bool registrarCanceladoEm,
        CancellationToken cancellationToken)
    {
        var assinatura = await ObterAssinaturaDoPayloadAsync(webhook, cancellationToken);
        if (assinatura is null)
        {
            webhook.ErroProcessamento = "Assinatura nao encontrada para o payload informado.";
            return;
        }

        if (assinatura.Status == novoStatus)
        {
            webhook.Processado = true;
            webhook.ProcessadoEm ??= DateTime.UtcNow;
            return;
        }

        assinatura.Status = novoStatus;
        assinatura.RenovacaoAutomatica = false;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.UpdatedAt = DateTime.UtcNow;

        if (registrarCanceladoEm)
        {
            assinatura.CanceladoEm ??= DateTime.UtcNow;
        }

        _assinaturaRepository.Atualizar(assinatura);

        webhook.Processado = true;
        webhook.ProcessadoEm = DateTime.UtcNow;
    }

    private async Task<Assinatura?> ObterAssinaturaDoPayloadAsync(
        WebhookPagamento webhook,
        CancellationToken cancellationToken)
    {
        var assinaturaId = ExtrairAssinaturaId(webhook.Payload);
        if (assinaturaId.HasValue)
        {
            return await _assinaturaRepository.ObterPorIdComPlanoAsync(assinaturaId.Value, cancellationToken);
        }

        var gatewayPaymentId = ExtrairGatewayPaymentId(webhook.Payload);
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            return null;
        }

        var pagamento = await _pagamentoRepository.ObterPorGatewayPaymentIdAsync(
            webhook.Gateway,
            gatewayPaymentId,
            cancellationToken);

        return pagamento?.Assinatura;
    }

    private static bool EventoPagamentoAprovado(string eventType) =>
        eventType.Equals("payment.approved", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.paid", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.paid", StringComparison.OrdinalIgnoreCase);

    private static bool EventoAssinaturaCancelada(string eventType) =>
        eventType.Equals("subscription.cancelled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("subscription.canceled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.cancelled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.canceled", StringComparison.OrdinalIgnoreCase);

    private static bool EventoAssinaturaSuspensa(string eventType) =>
        eventType.Equals("subscription.suspended", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.suspended", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("subscription.paused", StringComparison.OrdinalIgnoreCase);

    private static string? ExtrairGatewayPaymentId(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (TryGetString(root, "gatewayPaymentId", out var gatewayPaymentId))
            {
                return gatewayPaymentId;
            }

            if (TryGetString(root, "paymentId", out var paymentId))
            {
                return paymentId;
            }

            if (TryGetString(root, "id", out var id))
            {
                return id;
            }

            if (root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && TryGetString(data, "id", out var dataId))
            {
                return dataId;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static int? ExtrairAssinaturaId(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (TryGetInt(root, "assinaturaId", out var assinaturaId))
            {
                return assinaturaId;
            }

            if (TryGetInt(root, "subscriptionId", out var subscriptionId))
            {
                return subscriptionId;
            }

            if (root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && TryGetInt(data, "assinaturaId", out var dataAssinaturaId))
            {
                return dataAssinaturaId;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetInt(JsonElement element, string propertyName, out int? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            value = number;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out var stringNumber))
        {
            value = stringNumber;
            return true;
        }

        return false;
    }

    private static DateTime? CalcularFimAssinatura(DateTime inicio, PlanoPeriodo? periodo) =>
        periodo switch
        {
            PlanoPeriodo.Mensal => inicio.AddMonths(1),
            PlanoPeriodo.Trimestral => inicio.AddMonths(3),
            PlanoPeriodo.Semestral => inicio.AddMonths(6),
            PlanoPeriodo.Anual => inicio.AddYears(1),
            _ => null
        };
}
