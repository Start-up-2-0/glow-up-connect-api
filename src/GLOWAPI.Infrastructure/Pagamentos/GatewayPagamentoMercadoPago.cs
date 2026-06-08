using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CriarRequestUri("v1/payments"))
        {
            Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
        };
        AplicarHeadersMercadoPago(httpRequest, request.ReferenciaInterna);

        var envio = await TentarEnviarAsync(httpRequest, cancellationToken);
        if (!envio.Sucesso)
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                "{}",
                $"Falha ao chamar Mercado Pago: {envio.Erro}");
        }

        using var response = envio.Response!;
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CriarCobrancaGatewayResponse.Falha(
                requestPayload,
                responsePayload,
                MontarMensagemErroHttp((int)response.StatusCode, responsePayload, "criar pagamento"),
                CriarFailureInfo(response, responsePayload));
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
            CriarRequestUri($"v1/payments/{Uri.EscapeDataString(gatewayPaymentId.Trim())}"));
        AplicarHeadersMercadoPago(httpRequest);

        var envio = await TentarEnviarAsync(httpRequest, cancellationToken);
        if (!envio.Sucesso)
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                "{}",
                $"Falha ao chamar Mercado Pago: {envio.Erro}");
        }

        using var response = envio.Response!;
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ConsultarPagamentoGatewayResponse.Falha(
                gatewayPaymentId,
                responsePayload,
                MontarMensagemErroHttp((int)response.StatusCode, responsePayload, "consultar pagamento"));
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
                email = ResolverPagadorEmail(request.PagadorEmail),
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

    public async Task<CriarAssinaturaRecorrenteGatewayResponse> CriarAssinaturaRecorrenteAsync(
        CriarAssinaturaRecorrenteGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Gateway != GatewaySuportado)
        {
            var payloadIncompativel = SerializarAssinaturaRequest(request);
            return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                payloadIncompativel,
                "{}",
                $"Gateway incompativel. Esperado {GatewaySuportado}, recebido {request.Gateway}.");
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                SerializarAssinaturaRequest(request),
                "{}",
                "Access token do Mercado Pago nao configurado.");
        }

        if (request.PagamentoTransparente is null || string.IsNullOrWhiteSpace(request.PagamentoTransparente.Token))
        {
            return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                SerializarAssinaturaRequest(request),
                "{}",
                "Token do cartao e obrigatorio para criar assinatura recorrente no Mercado Pago.");
        }

        string? preapprovalPlanId = TextoOuNull(_options.PreapprovalPlanId);

        if (request.DiasTrial is > 0)
        {
            if (string.IsNullOrWhiteSpace(preapprovalPlanId))
            {
                var plano = await CriarPlanoTrialAsync(request, cancellationToken);
                if (!plano.Sucesso)
                {
                    return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                        plano.RequestPayload,
                        plano.ResponsePayload,
                        plano.MensagemErro ?? "Nao foi possivel criar plano de assinatura no Mercado Pago.",
                        plano.FailureInfo);
                }

                preapprovalPlanId = plano.PlanId;
            }
        }

        var payload = CriarPayloadAssinatura(request, preapprovalPlanId);
        var requestPayload = JsonSerializer.Serialize(payload, JsonOptions);
        var envioAssinatura = await PostMercadoPagoComRetryAsync(
            "preapproval",
            requestPayload,
            request.ReferenciaInterna,
            cancellationToken);

        if (!envioAssinatura.Sucesso)
        {
            return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                requestPayload,
                envioAssinatura.ResponsePayload,
                envioAssinatura.MensagemErro ?? "Nao foi possivel criar assinatura recorrente no Mercado Pago.",
                envioAssinatura.FailureInfo);
        }

        using var document = JsonDocument.Parse(envioAssinatura.ResponsePayload);
        var root = document.RootElement;
        var subscriptionId = ObterString(root, "id");
        var payerId = ObterString(root, "payer_id");

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return CriarAssinaturaRecorrenteGatewayResponse.Falha(
                requestPayload,
                envioAssinatura.ResponsePayload,
                "Mercado Pago nao retornou id da assinatura recorrente.");
        }

        return new CriarAssinaturaRecorrenteGatewayResponse(
            Sucesso: true,
            GatewaySubscriptionId: subscriptionId,
            GatewayCustomerId: payerId,
            RequestPayload: requestPayload,
            ResponsePayload: envioAssinatura.ResponsePayload);
    }

    private async Task<(bool Sucesso, string? PlanId, string RequestPayload, string ResponsePayload, string? MensagemErro, GatewayHttpFailureInfo? FailureInfo)>
        CriarPlanoTrialAsync(
            CriarAssinaturaRecorrenteGatewayRequest request,
            CancellationToken cancellationToken)
    {
        var payload = CriarPayloadPlanoTrial(request);
        var requestPayload = JsonSerializer.Serialize(payload, JsonOptions);
        var envio = await PostMercadoPagoComRetryAsync(
            "preapproval_plan",
            requestPayload,
            $"{request.ReferenciaInterna}-plan",
            cancellationToken);

        if (!envio.Sucesso)
        {
            return (false, null, requestPayload, envio.ResponsePayload, envio.MensagemErro, envio.FailureInfo);
        }

        using var document = JsonDocument.Parse(envio.ResponsePayload);
        var planId = ObterString(document.RootElement, "id");
        if (string.IsNullOrWhiteSpace(planId))
        {
            return (
                false,
                null,
                requestPayload,
                envio.ResponsePayload,
                "Mercado Pago nao retornou id do plano de assinatura.",
                null);
        }

        return (true, planId, requestPayload, envio.ResponsePayload, null, null);
    }

    private async Task<(bool Sucesso, string ResponsePayload, string? MensagemErro, GatewayHttpFailureInfo? FailureInfo)>
        PostMercadoPagoComRetryAsync(
            string path,
            string requestPayload,
            string idempotencyPrefix,
            CancellationToken cancellationToken)
    {
        const int maxTentativas = 3;
        HttpResponseMessage? response = null;
        var responsePayload = string.Empty;

        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CriarRequestUri(path))
            {
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
            };
            AplicarHeadersMercadoPago(httpRequest, $"{idempotencyPrefix}-{tentativa}");

            var envio = await TentarEnviarAsync(httpRequest, cancellationToken);
            if (!envio.Sucesso)
            {
                return (false, "{}", $"Falha ao chamar Mercado Pago: {envio.Erro}", null);
            }

            response = envio.Response!;
            responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                break;
            }

            if ((int)response.StatusCode != 503 || tentativa == maxTentativas)
            {
                var operacao = path.Contains("preapproval_plan", StringComparison.OrdinalIgnoreCase)
                    ? "criar plano de assinatura"
                    : "criar assinatura recorrente";
                var failureInfo = CriarFailureInfo(response, responsePayload);
                response.Dispose();
                return (
                    false,
                    responsePayload,
                    MontarMensagemErroHttp((int)response.StatusCode, responsePayload, operacao),
                    failureInfo);
            }

            response.Dispose();
            response = null;
            await Task.Delay(TimeSpan.FromSeconds(tentativa), cancellationToken);
        }

        response!.Dispose();
        return (true, responsePayload, null, null);
    }

    private object CriarPayloadPlanoTrial(CriarAssinaturaRecorrenteGatewayRequest request)
    {
        var (freeTrialFrequency, freeTrialFrequencyType) = ResolverFreeTrial(request.DiasTrial);
        var autoRecurring = new Dictionary<string, object?>
        {
            ["frequency"] = 1,
            ["frequency_type"] = "months",
            ["repetitions"] = 120,
            ["transaction_amount"] = request.Valor,
            ["currency_id"] = request.Moeda,
            ["free_trial"] = new Dictionary<string, object?>
            {
                ["frequency"] = freeTrialFrequency,
                ["frequency_type"] = freeTrialFrequencyType
            }
        };

        var diaVencimento = ObterDiaVencimento(request.Metadados);
        if (diaVencimento.HasValue)
        {
            autoRecurring["billing_day"] = diaVencimento.Value;
            autoRecurring["billing_day_proportional"] = false;
        }

        return new
        {
            reason = request.Descricao,
            auto_recurring = autoRecurring,
            back_url = TextoOuNull(_options.SuccessUrl)
        };
    }

    private static (int Frequency, string FrequencyType) ResolverFreeTrial(int? diasTrial) =>
        diasTrial switch
        {
            30 => (1, "months"),
            > 0 => (diasTrial.Value, "days"),
            _ => (0, "days")
        };

    private object CriarPayloadAssinatura(
        CriarAssinaturaRecorrenteGatewayRequest request,
        string? preapprovalPlanId)
    {
        if (!string.IsNullOrWhiteSpace(preapprovalPlanId))
        {
            return new
            {
                preapproval_plan_id = preapprovalPlanId,
                reason = request.Descricao,
                external_reference = request.ReferenciaInterna,
                payer_email = ResolverPagadorEmail(request.PagadorEmail),
                card_token_id = request.PagamentoTransparente!.Token,
                back_url = TextoOuNull(_options.SuccessUrl),
                status = "authorized"
            };
        }

        var autoRecurring = new Dictionary<string, object?>
        {
            ["frequency"] = 1,
            ["frequency_type"] = "months",
            ["transaction_amount"] = request.Valor,
            ["currency_id"] = request.Moeda
        };

        if (request.PrimeiraCobrancaEm.HasValue)
        {
            var inicio = NormalizarDataInicioRecorrencia(request.PrimeiraCobrancaEm.Value);
            autoRecurring["start_date"] = FormatarDataMercadoPago(inicio);
            autoRecurring["end_date"] = FormatarDataMercadoPago(inicio.AddYears(10));
        }

        return new
        {
            reason = request.Descricao,
            external_reference = request.ReferenciaInterna,
            payer_email = ResolverPagadorEmail(request.PagadorEmail),
            card_token_id = request.PagamentoTransparente!.Token,
            auto_recurring = autoRecurring,
            back_url = TextoOuNull(_options.SuccessUrl),
            status = "authorized"
        };
    }

    private static int? ObterDiaVencimento(IReadOnlyDictionary<string, string>? metadados)
    {
        if (metadados is null || !metadados.TryGetValue("diaVencimento", out var dia))
        {
            return null;
        }

        return int.TryParse(dia, out var valor) && valor is >= 1 and <= 28 ? valor : null;
    }

    private async Task<(bool Sucesso, HttpResponseMessage? Response, string? Erro)> TentarEnviarAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return (true, response, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            return (false, null, ex.Message);
        }
    }

    private string ResolverPagadorEmail(string pagadorEmail) =>
        MercadoPagoPayerEmailResolver.Resolver(pagadorEmail, _options);

    private static DateTime NormalizarDataInicioRecorrencia(DateTime data) =>
        data.ToUniversalTime() switch
        {
            var utc when utc.TimeOfDay == TimeSpan.Zero => utc.AddHours(12),
            var utc => utc
        };

    private static string FormatarDataMercadoPago(DateTime dataUtc) =>
        dataUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private void AplicarHeadersMercadoPago(HttpRequestMessage request, string? idempotencyKey = null)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        }

        if (_options.AccessToken.TrimStart().StartsWith("TEST-", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Add("X-scope", "stage");
        }
    }

    private static GatewayHttpFailureInfo CriarFailureInfo(
        HttpResponseMessage response,
        string responsePayload)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return new GatewayHttpFailureInfo(
            HttpStatusCode: (int)response.StatusCode,
            RequestUri: response.RequestMessage?.RequestUri?.ToString(),
            ResponseHeaders: headers.Count == 0 ? null : headers);
    }

    private static string MontarMensagemErroHttp(int statusCode, string responsePayload, string operacao)
    {
        var detalhe = ExtrairMensagemRespostaMercadoPago(responsePayload);
        return string.IsNullOrWhiteSpace(detalhe)
            ? $"Mercado Pago retornou {statusCode} ao {operacao}."
            : $"Mercado Pago retornou {statusCode} ao {operacao}: {detalhe}";
    }

    private static string? ExtrairMensagemRespostaMercadoPago(string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responsePayload);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            if (root.TryGetProperty("cause", out var cause)
                && cause.ValueKind == JsonValueKind.Array
                && cause.GetArrayLength() > 0)
            {
                var first = cause[0];
                if (first.TryGetProperty("description", out var description)
                    && description.ValueKind == JsonValueKind.String)
                {
                    return description.GetString();
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private Uri CriarRequestUri(string path)
    {
        var relativePath = path.TrimStart('/');
        var apiBaseUrl = string.IsNullOrWhiteSpace(_options.ApiBaseUrl)
            ? "https://api.mercadopago.com"
            : _options.ApiBaseUrl.Trim().TrimEnd('/');
        return new Uri($"{apiBaseUrl}/{relativePath}");
    }

    private static string SerializarAssinaturaRequest(CriarAssinaturaRecorrenteGatewayRequest request) =>
        JsonSerializer.Serialize(new
        {
            gateway = request.Gateway.ToString(),
            reference = request.ReferenciaInterna,
            description = request.Descricao,
            amount = request.Valor,
            currency = request.Moeda,
            diasTrial = request.DiasTrial,
            primeiraCobrancaEm = request.PrimeiraCobrancaEm,
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            metadata = request.Metadados
        }, JsonOptions);

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
