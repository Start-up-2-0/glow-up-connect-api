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
    private readonly IGatewayPagamentoResolver _gatewayPagamentoResolver;
    private readonly ICobrancaAssinaturaService _cobrancaAssinaturaService;
    private readonly IAssinaturaHistoricoService _assinaturaHistoricoService;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;

    public WebhookPagamentoService(
        IWebhookPagamentoRepository webhookPagamentoRepository,
        IPagamentoRepository pagamentoRepository,
        IAssinaturaRepository assinaturaRepository,
        IGatewayPagamentoResolver gatewayPagamentoResolver,
        ICobrancaAssinaturaService cobrancaAssinaturaService,
        IAssinaturaHistoricoService assinaturaHistoricoService,
        IAssinaturaNotificacaoService assinaturaNotificacaoService)
    {
        _webhookPagamentoRepository = webhookPagamentoRepository;
        _pagamentoRepository = pagamentoRepository;
        _assinaturaRepository = assinaturaRepository;
        _gatewayPagamentoResolver = gatewayPagamentoResolver;
        _cobrancaAssinaturaService = cobrancaAssinaturaService;
        _assinaturaHistoricoService = assinaturaHistoricoService;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
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

        if (EventoAssinaturaAutorizada(webhook.EventType))
        {
            webhook.Processado = true;
            webhook.ProcessadoEm = DateTime.UtcNow;
            return;
        }

        if (EventoPagamentoNaoAprovado(webhook.EventType))
        {
            await ProcessarPagamentoNaoAprovadoAsync(webhook, cancellationToken);
            return;
        }

        if (EventoPagamentoParaConsultaNoGateway(webhook.EventType))
        {
            await ProcessarPagamentoConsultandoGatewayAsync(webhook, cancellationToken);
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

        var pagamento = await ObterPagamentoDoWebhookAsync(webhook, gatewayPaymentId, cancellationToken);

        if (pagamento is null)
        {
            webhook.ErroProcessamento = "Pagamento nao encontrado para o gatewayPaymentId informado.";
            return;
        }

        await _cobrancaAssinaturaService.ProcessarPagamentoAprovadoAsync(
            pagamento,
            webhook.Payload,
            cancellationToken);

        webhook.Processado = true;
        webhook.ProcessadoEm = DateTime.UtcNow;
    }

    private async Task ProcessarPagamentoConsultandoGatewayAsync(
        WebhookPagamento webhook,
        CancellationToken cancellationToken)
    {
        var gatewayPaymentId = ExtrairGatewayPaymentId(webhook.Payload);
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            webhook.ErroProcessamento = "Payload nao contem gatewayPaymentId.";
            return;
        }

        var gateway = _gatewayPagamentoResolver.Resolver(webhook.Gateway);
        var consulta = await gateway.ConsultarPagamentoAsync(gatewayPaymentId, cancellationToken);
        if (!consulta.Sucesso)
        {
            webhook.ErroProcessamento = consulta.MensagemErro ?? "Nao foi possivel consultar pagamento no gateway.";
            return;
        }

        var eventType = ConverterStatusGatewayParaEvento(consulta.Status);
        if (eventType is null)
        {
            webhook.Processado = true;
            webhook.ProcessadoEm = DateTime.UtcNow;
            return;
        }

        var payloadNormalizado = JsonSerializer.Serialize(new
        {
            gatewayPaymentId = consulta.GatewayPaymentId,
            email = consulta.PagadorEmail
        });
        var webhookNormalizado = new WebhookPagamento
        {
            Gateway = webhook.Gateway,
            EventId = webhook.EventId,
            EventType = eventType,
            Payload = payloadNormalizado
        };

        await ProcessarAsync(webhookNormalizado, cancellationToken);

        webhook.Processado = webhookNormalizado.Processado;
        webhook.ProcessadoEm = webhookNormalizado.ProcessadoEm;
        webhook.ErroProcessamento = webhookNormalizado.ErroProcessamento;
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

        var statusAnterior = assinatura.Status;
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
        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            novoStatus == AssinaturaStatus.Cancelada ? "AssinaturaCanceladaPorWebhook" : "AssinaturaSuspensaPorWebhook",
            statusAnterior,
            assinatura.Status,
            observacao: "Status alterado por webhook do gateway.",
            payloadJson: webhook.Payload,
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            novoStatus == AssinaturaStatus.Cancelada ? "RecorrenciaCanceladaPorWebhook" : "RecorrenciaSuspensaPorWebhook",
            novoStatus.ToString(),
            cicloInicio: assinatura.Inicio,
            cicloFim: assinatura.Fim,
            observacao: "Recorrencia alterada por webhook do gateway.",
            payloadJson: webhook.Payload,
            cancellationToken: cancellationToken);

        if (novoStatus == AssinaturaStatus.Cancelada)
        {
            await _assinaturaNotificacaoService.AssinaturaCanceladaAsync(
                assinatura,
                ExtrairEmail(webhook.Payload),
                cancellationToken);
        }
        else
        {
            await _assinaturaNotificacaoService.AssinaturaSuspensaAsync(
                assinatura,
                ExtrairEmail(webhook.Payload),
                cancellationToken);
        }

        webhook.Processado = true;
        webhook.ProcessadoEm = DateTime.UtcNow;
    }

    private async Task ProcessarPagamentoNaoAprovadoAsync(
        WebhookPagamento webhook,
        CancellationToken cancellationToken)
    {
        var gatewayPaymentId = ExtrairGatewayPaymentId(webhook.Payload);
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            webhook.ErroProcessamento = "Payload nao contem gatewayPaymentId.";
            return;
        }

        var pagamento = await ObterPagamentoDoWebhookAsync(webhook, gatewayPaymentId, cancellationToken);

        if (pagamento is null)
        {
            webhook.ErroProcessamento = "Pagamento nao encontrado para o gatewayPaymentId informado.";
            return;
        }

        await _cobrancaAssinaturaService.ProcessarPagamentoRecusadoAsync(
            pagamento,
            webhook.Payload,
            ObterStatusPagamentoNaoAprovado(webhook.EventType),
            cancellationToken);

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

        var pagamento = await ObterPagamentoDoWebhookAsync(webhook, gatewayPaymentId, cancellationToken);
        return pagamento?.Assinatura;
    }

    private async Task<Pagamento?> ObterPagamentoDoWebhookAsync(
        WebhookPagamento webhook,
        string gatewayPaymentId,
        CancellationToken cancellationToken)
    {
        var pagamento = await _pagamentoRepository.ObterPorGatewayPaymentIdAsync(
            webhook.Gateway,
            gatewayPaymentId,
            cancellationToken);

        if (pagamento is not null)
        {
            if (!string.Equals(pagamento.GatewayPaymentId, gatewayPaymentId, StringComparison.Ordinal))
            {
                pagamento.GatewayPaymentId = gatewayPaymentId;
                _pagamentoRepository.Atualizar(pagamento);
            }

            return pagamento;
        }

        var gateway = _gatewayPagamentoResolver.Resolver(webhook.Gateway);
        var consulta = await gateway.ConsultarPagamentoAsync(gatewayPaymentId, cancellationToken);
        if (!consulta.Sucesso || string.IsNullOrWhiteSpace(consulta.ReferenciaExterna))
        {
            return null;
        }

        pagamento = await _pagamentoRepository.ObterPorReferenciaInternaAsync(
            consulta.ReferenciaExterna,
            cancellationToken);

        if (pagamento is null)
        {
            return null;
        }

        pagamento.GatewayPaymentId = gatewayPaymentId;
        _pagamentoRepository.Atualizar(pagamento);
        return pagamento;
    }

    private static bool EventoPagamentoAprovado(string eventType) =>
        eventType.Equals("payment.approved", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.paid", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.paid", StringComparison.OrdinalIgnoreCase);

    private static bool EventoAssinaturaAutorizada(string eventType) =>
        eventType.Equals("subscription.authorized", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("preapproval.authorized", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.authorized", StringComparison.OrdinalIgnoreCase);

    private static bool EventoAssinaturaCancelada(string eventType) =>
        eventType.Equals("subscription.cancelled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("subscription.canceled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.cancelled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.canceled", StringComparison.OrdinalIgnoreCase);

    private static bool EventoAssinaturaSuspensa(string eventType) =>
        eventType.Equals("subscription.suspended", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("billing.suspended", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("subscription.paused", StringComparison.OrdinalIgnoreCase);

    private static bool EventoPagamentoNaoAprovado(string eventType) =>
        eventType.Equals("payment.rejected", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.refused", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.failed", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.cancelled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.canceled", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.expired", StringComparison.OrdinalIgnoreCase);

    private static bool EventoPagamentoParaConsultaNoGateway(string eventType) =>
        eventType.Equals("payment", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.created", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.updated", StringComparison.OrdinalIgnoreCase)
        || eventType.Equals("payment.updated_webhook", StringComparison.OrdinalIgnoreCase);

    private static string? ConverterStatusGatewayParaEvento(string status) =>
        status.ToLowerInvariant() switch
        {
            "approved" or "accredited" => "payment.approved",
            "rejected" => "payment.rejected",
            "cancelled" or "canceled" => "payment.canceled",
            "expired" => "payment.expired",
            _ => null
        };

    private static PagamentoStatus ObterStatusPagamentoNaoAprovado(string eventType)
    {
        if (eventType.Equals("payment.cancelled", StringComparison.OrdinalIgnoreCase)
            || eventType.Equals("payment.canceled", StringComparison.OrdinalIgnoreCase))
        {
            return PagamentoStatus.Cancelado;
        }

        if (eventType.Equals("payment.expired", StringComparison.OrdinalIgnoreCase))
        {
            return PagamentoStatus.Expirado;
        }

        return PagamentoStatus.Recusado;
    }

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

            if (root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && TryGetString(data, "id", out var dataId))
            {
                return dataId;
            }

            if (TryGetString(root, "id", out var id))
            {
                return id;
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

    private static string? ExtrairEmail(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (TryGetString(root, "email", out var email))
            {
                return email;
            }

            if (TryGetString(root, "payerEmail", out var payerEmail))
            {
                return payerEmail;
            }

            if (TryGetString(root, "pagadorEmail", out var pagadorEmail))
            {
                return pagadorEmail;
            }

            if (TryGetString(root, "customerEmail", out var customerEmail))
            {
                return customerEmail;
            }

            if (root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && TryGetString(data, "email", out var dataEmail))
            {
                return dataEmail;
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
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        value = property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
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

}
