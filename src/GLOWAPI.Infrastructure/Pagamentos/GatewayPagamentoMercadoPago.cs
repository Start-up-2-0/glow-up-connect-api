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

        var payload = CriarPayload(request);
        var requestPayload = JsonSerializer.Serialize(payload, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences")
        {
            Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                $"Mercado Pago retornou {(int)response.StatusCode} ao criar preferencia de pagamento.");
        }

        using var document = JsonDocument.Parse(responsePayload);
        var root = document.RootElement;
        var preferenceId = ObterString(root, "id");
        var checkoutUrl = ObterString(root, "init_point")
            ?? ObterString(root, "sandbox_init_point")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(preferenceId) || string.IsNullOrWhiteSpace(checkoutUrl))
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                "Mercado Pago nao retornou id ou URL de checkout da preferencia.");
        }

        return new CriarCobrancaGatewayResponse(
            Sucesso: true,
            GatewayPaymentId: preferenceId,
            CheckoutUrl: checkoutUrl,
            QrCode: string.Empty,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload);
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
        var backUrls = CriarBackUrls();

        return new
        {
            items = new[]
            {
                new
                {
                    id = request.ReferenciaInterna,
                    title = request.Descricao,
                    quantity = 1,
                    currency_id = request.Moeda,
                    unit_price = request.Valor
                }
            },
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            external_reference = request.ReferenciaInterna,
            notification_url = TextoOuNull(_options.NotificationUrl),
            back_urls = backUrls,
            auto_return = backUrls is null ? null : "approved",
            payment_methods = CriarPaymentMethods(request.MetodoPagamento),
            expires = request.ExpiraEm.HasValue,
            expiration_date_to = request.ExpiraEm,
            metadata = request.Metadados
        };
    }

    private object? CriarBackUrls()
    {
        var success = TextoOuNull(_options.SuccessUrl);
        var failure = TextoOuNull(_options.FailureUrl);
        var pending = TextoOuNull(_options.PendingUrl);

        if (success is null && failure is null && pending is null)
        {
            return null;
        }

        return new
        {
            success,
            failure,
            pending
        };
    }

    private static object CriarPaymentMethods(MetodoPagamentoAssinatura metodoPagamento)
    {
        var excludedPaymentTypes = metodoPagamento switch
        {
            MetodoPagamentoAssinatura.Pix => new[] { "credit_card", "debit_card", "ticket" },
            MetodoPagamentoAssinatura.Boleto => new[] { "credit_card", "debit_card", "pix" },
            MetodoPagamentoAssinatura.Cartao => new[] { "pix", "ticket" },
            _ => Array.Empty<string>()
        };

        return new
        {
            excluded_payment_types = excludedPaymentTypes
                .Select(id => new { id })
                .ToArray()
        };
    }

    private static string? TextoOuNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ObterString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
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
            paymentMethod = request.MetodoPagamento.ToString(),
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            expiresAt = request.ExpiraEm,
            metadata = request.Metadados
        }, JsonOptions);
}
