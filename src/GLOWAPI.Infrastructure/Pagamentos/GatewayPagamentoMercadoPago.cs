using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Pagamentos;

public class GatewayPagamentoMercadoPago : IGatewayPagamento
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;

    public GatewayPagamentoMercadoPago(
        HttpClient httpClient,
        IOptions<MercadoPagoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public GatewayPagamento GatewaySuportado => GatewayPagamento.MercadoPago;

    public async Task<CriarCobrancaGatewayResponse> CriarCobrancaAsync(
        CriarCobrancaGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Gateway != GatewaySuportado)
        {
            var payloadIncompativel = SerializarRequest(request);
            return CriarCobrancaGatewayResponse.Falha(
                payloadIncompativel,
                "{}",
                $"Gateway incompativel. Esperado {GatewaySuportado}, recebido {request.Gateway}.");
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return CriarCobrancaGatewayResponse.Falha(
                SerializarRequest(request),
                "{}",
                "Access token do Mercado Pago nao configurado.");
        }

        if (request.PagamentoTransparente is null)
        {
            return CriarCobrancaGatewayResponse.Falha(
                SerializarRequest(request),
                "{}",
                "Dados do Checkout Transparente sao obrigatorios para criar pagamento no Mercado Pago.");
        }

        var payload = CriarPayload(request);
        var requestPayload = JsonSerializer.Serialize(payload, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
        {
            Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        httpRequest.Headers.Add("X-Idempotency-Key", request.ReferenciaInterna);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                $"Mercado Pago retornou {(int)response.StatusCode} ao criar pagamento.");
        }

        using var document = JsonDocument.Parse(responsePayload);
        var root = document.RootElement;
        var paymentId = ObterString(root, "id");
        var status = ObterString(root, "status");
        var checkoutUrl = ObterString(root, "transaction_details.external_resource_url")
            ?? ObterString(root, "point_of_interaction.transaction_data.ticket_url")
            ?? string.Empty;
        var qrCode = ObterString(root, "point_of_interaction.transaction_data.qr_code")
            ?? ObterString(root, "point_of_interaction.transaction_data.qr_code_base64")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                "Mercado Pago nao retornou id do pagamento.");
        }

        if (string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            var statusDetail = ObterString(root, "status_detail");
            var mensagemErro = string.IsNullOrWhiteSpace(statusDetail)
                ? "Pagamento recusado pelo Mercado Pago."
                : $"Pagamento recusado pelo Mercado Pago: {statusDetail}.";

            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                mensagemErro);
        }

        return new CriarCobrancaGatewayResponse(
            Sucesso: true,
            GatewayPaymentId: paymentId,
            CheckoutUrl: checkoutUrl,
            QrCode: qrCode,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            MetodoPagamento: request.PagamentoTransparente.PaymentMethodId);
    }

    public async Task<ConsultarPagamentoGatewayResponse> ConsultarPagamentoAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                "{}",
                "GatewayPaymentId e obrigatorio para consulta no Mercado Pago.");
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                "{}",
                "Access token do Mercado Pago nao configurado.");
        }

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"v1/payments/{Uri.EscapeDataString(gatewayPaymentId.Trim())}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                responsePayload,
                $"Mercado Pago retornou {(int)response.StatusCode} ao consultar pagamento.");
        }

        using var document = JsonDocument.Parse(responsePayload);
        var root = document.RootElement;
        var status = ObterString(root, "status");
        var paymentId = ObterString(root, "id") ?? gatewayPaymentId;
        var pagadorEmail = ExtrairPagadorEmail(root);

        if (string.IsNullOrWhiteSpace(status))
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                responsePayload,
                "Mercado Pago nao retornou status do pagamento.");
        }

        return new ConsultarPagamentoGatewayResponse(
            Sucesso: true,
            GatewayPaymentId: paymentId,
            Status: status,
            ResponsePayload: responsePayload,
            PagadorEmail: pagadorEmail);
    }

    private object CriarPayload(CriarCobrancaGatewayRequest request)
    {
        var pagamento = request.PagamentoTransparente!;

        return new
        {
            transaction_amount = request.Valor,
            description = request.Descricao,
            payment_method_id = pagamento.PaymentMethodId,
            token = TextoOuNull(pagamento.Token),
            issuer_id = TextoOuNull(pagamento.IssuerId),
            installments = pagamento.Installments,
            payer = new
            {
                email = request.PagadorEmail,
                first_name = request.PagadorNome,
                identification = CriarIdentificacao(pagamento)
            },
            external_reference = request.ReferenciaInterna,
            notification_url = TextoOuNull(_options.NotificationUrl),
            metadata = request.Metadados
        };
    }

    private static object? CriarIdentificacao(PagamentoTransparenteGatewayRequest pagamento)
    {
        var type = TextoOuNull(pagamento.IdentificationType);
        var number = TextoOuNull(pagamento.IdentificationNumber);
        if (type is null || number is null)
        {
            return null;
        }

        return new
        {
            type,
            number
        };
    }

    private static string? TextoOuNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ObterString(JsonElement element, string propertyName)
    {
        var current = element;
        foreach (var segment in propertyName.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        var property = current;
        if (property.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static string? ExtrairPagadorEmail(JsonElement root)
    {
        if (root.TryGetProperty("payer", out var payer)
            && payer.ValueKind == JsonValueKind.Object
            && ObterString(payer, "email") is { } payerEmail)
        {
            return payerEmail;
        }

        return null;
    }

    private static string SerializarRequest(CriarCobrancaGatewayRequest request) =>
        JsonSerializer.Serialize(new
        {
            gateway = request.Gateway.ToString(),
            reference = request.ReferenciaInterna,
            description = request.Descricao,
            amount = request.Valor,
            currency = request.Moeda,
            paymentMethod = request.PagamentoTransparente?.PaymentMethodId,
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            expiresAt = request.ExpiraEm,
            metadata = request.Metadados
        }, JsonOptions);
}
