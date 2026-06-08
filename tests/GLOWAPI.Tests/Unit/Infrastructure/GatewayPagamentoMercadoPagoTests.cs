using System.Net;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Pagamentos;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class GatewayPagamentoMercadoPagoTests
{
    [Fact]
    public async Task CriarCobrancaAsync_DeveCriarPagamentoTransparenteNoMercadoPago()
    {
        HttpRequestMessage? requestMessage = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestMessage = request;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """
                    {
                      "id": 123456,
                      "status": "pending",
                      "point_of_interaction": {
                        "transaction_data": {
                          "qr_code": "000201..."
                        }
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com",
            NotificationUrl = "https://api.glow.test/api/webhooks/pagamentos"
        });

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            Metadados: new Dictionary<string, string>
            {
                ["planoId"] = "3"
            },
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest(
                PaymentMethodId: "pix",
                IdentificationType: "CPF",
                IdentificationNumber: "12345678909")));

        Assert.True(response.Sucesso);
        Assert.Equal("123456", response.GatewayPaymentId);
        Assert.Equal("000201...", response.QrCode);
        Assert.Equal("pix", response.MetodoPagamento);
        Assert.NotNull(requestMessage);
        Assert.Equal(HttpMethod.Post, requestMessage!.Method);
        Assert.Equal("Bearer", requestMessage.Headers.Authorization?.Scheme);
        Assert.Equal("TEST-123", requestMessage.Headers.Authorization?.Parameter);
        Assert.Equal("assinatura-abc", requestMessage.Headers.GetValues("X-Idempotency-Key").Single());
        Assert.Equal("https://api.mercadopago.com/v1/payments", requestMessage.RequestUri?.ToString());

        using var json = JsonDocument.Parse(response.RequestPayload);
        var root = json.RootElement;
        Assert.Equal("assinatura-abc", root.GetProperty("external_reference").GetString());
        Assert.Equal("https://api.glow.test/api/webhooks/pagamentos", root.GetProperty("notification_url").GetString());
        Assert.Equal("pix", root.GetProperty("payment_method_id").GetString());
        Assert.Equal("maria@email.com", root.GetProperty("payer").GetProperty("email").GetString());
        Assert.Equal(199.90m, root.GetProperty("transaction_amount").GetDecimal());
    }

    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarFalha_QuandoDadosTransparentesNaoForemInformados()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Nao deveria chamar HTTP."));
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com"));

        Assert.False(response.Sucesso);
        Assert.Contains("Checkout Transparente", response.MensagemErro);
    }

    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarFalha_QuandoAccessTokenNaoEstiverConfigurado()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Nao deveria chamar HTTP."));
        var gateway = CriarGateway(handler, new MercadoPagoOptions());

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("pix")));

        Assert.False(response.Sucesso);
        Assert.Equal(string.Empty, response.GatewayPaymentId);
        Assert.Contains("Access token do Mercado Pago nao configurado", response.MensagemErro);
    }

    [Fact]
    public async Task CriarCobrancaAsync_NaoDeveEnviarNotificationUrl_QuandoConfiguracaoEstiverVazia()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":123456,"status":"pending"}""",
                Encoding.UTF8,
                "application/json")
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("pix")));

        using var json = JsonDocument.Parse(response.RequestPayload);
        Assert.False(json.RootElement.TryGetProperty("notification_url", out _));
    }

    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarFalha_QuandoMercadoPagoRecusarPagamento()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """
                {
                  "id": 123456,
                  "status": "rejected",
                  "status_detail": "cc_rejected_insufficient_amount"
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("master")));

        Assert.False(response.Sucesso);
        Assert.Equal(string.Empty, response.GatewayPaymentId);
        Assert.Contains("Pagamento recusado pelo Mercado Pago", response.MensagemErro);
        Assert.Contains("cc_rejected_insufficient_amount", response.MensagemErro);
        Assert.Contains("\"status\": \"rejected\"", response.ResponsePayload);
    }

    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarFalha_QuandoMercadoPagoRetornarErro()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"invalid payer"}""", Encoding.UTF8, "application/json")
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-abc",
            Descricao: "Assinatura Premium",
            Valor: 199.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("pix")));

        Assert.False(response.Sucesso);
        Assert.Contains("Mercado Pago retornou 400", response.MensagemErro);
        Assert.Contains("invalid payer", response.ResponsePayload);
    }

    [Fact]
    public async Task ConsultarPagamentoAsync_DeveRetornarStatusDoPagamento()
    {
        HttpRequestMessage? requestMessage = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestMessage = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": 123456,
                      "status": "approved",
                      "payer": {
                        "email": "cliente@email.com"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.ConsultarPagamentoAsync("123456");

        Assert.True(response.Sucesso);
        Assert.Equal("123456", response.GatewayPaymentId);
        Assert.Equal("approved", response.Status);
        Assert.Equal("cliente@email.com", response.PagadorEmail);
        Assert.NotNull(requestMessage);
        Assert.Equal(HttpMethod.Get, requestMessage!.Method);
        Assert.Equal("https://api.mercadopago.com/v1/payments/123456", requestMessage.RequestUri?.ToString());
    }

    [Fact]
    public async Task ConsultarPagamentoAsync_DeveRetornarFalha_QuandoMercadoPagoRetornarErro()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"message":"payment not found"}""", Encoding.UTF8, "application/json")
        });
        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.ConsultarPagamentoAsync("123456");

        Assert.False(response.Sucesso);
        Assert.Contains("Mercado Pago retornou 404", response.MensagemErro);
        Assert.Contains("payment not found", response.MensagemErro);
        Assert.Contains("payment not found", response.ResponsePayload);
    }

    [Fact]
    public async Task CriarAssinaturaRecorrenteAsync_DeveUsarApiBaseUrlDasOptions_QuandoHttpClientNaoTiverBaseAddress()
    {
        HttpRequestMessage? requestMessage = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestMessage = request;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id":"sub-123","payer_id":"payer-1"}""", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var gateway = new GatewayPagamentoMercadoPago(httpClient, Options.Create(new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        }));

        var response = await gateway.CriarAssinaturaRecorrenteAsync(new CriarAssinaturaRecorrenteGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "trial-1",
            Descricao: "Assinatura trial",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Cliente",
            PagadorEmail: "cliente@email.com",
            DiasTrial: 30,
            PrimeiraCobrancaEm: DateTime.UtcNow.AddDays(30),
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("visa", "card-token", null, 1, "CPF", "12345678901"),
            Metadados: new Dictionary<string, string>()));

        Assert.True(response.Sucesso);
        Assert.NotNull(requestMessage);
        Assert.Equal("https://api.mercadopago.com/preapproval", requestMessage!.RequestUri?.ToString());
        Assert.Equal("stage", requestMessage.Headers.GetValues("X-scope").Single());
    }

    [Fact]
    public async Task CriarAssinaturaRecorrenteAsync_DeveUsarSuccessUrlComoBackUrl()
    {
        string? requestPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestPayload = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id":"sub-456","payer_id":"payer-2"}""", Encoding.UTF8, "application/json")
            };
        });

        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com",
            NotificationUrl = "https://api.glow.test/api/webhooks/pagamentos/mercado-pago",
            SuccessUrl = "https://front.glow.test/assinatura/sucesso"
        });

        var response = await gateway.CriarAssinaturaRecorrenteAsync(new CriarAssinaturaRecorrenteGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "trial-2",
            Descricao: "Assinatura trial",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Cliente",
            PagadorEmail: "cliente@email.com",
            DiasTrial: 30,
            PrimeiraCobrancaEm: DateTime.UtcNow.AddDays(30),
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("visa", "card-token", null, 1, "CPF", "12345678901"),
            Metadados: new Dictionary<string, string>()));

        Assert.True(response.Sucesso);
        Assert.NotNull(requestPayload);
        using var document = JsonDocument.Parse(requestPayload!);
        Assert.Equal(
            "https://front.glow.test/assinatura/sucesso",
            document.RootElement.GetProperty("back_url").GetString());
        Assert.False(document.RootElement.TryGetProperty("notification_url", out _));
    }

    [Fact]
    public async Task CriarAssinaturaRecorrenteAsync_DeveUsarPayerEmailOverride_QuandoConfigurado()
    {
        string? requestPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestPayload = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id":"sub-789","payer_id":"payer-3"}""", Encoding.UTF8, "application/json")
            };
        });

        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com",
            PayerEmailOverride = "TESTUSER978765836"
        });

        var response = await gateway.CriarAssinaturaRecorrenteAsync(new CriarAssinaturaRecorrenteGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "trial-3",
            Descricao: "Assinatura trial",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Cliente",
            PagadorEmail: "usuario.real@glow.com",
            DiasTrial: 30,
            PrimeiraCobrancaEm: DateTime.UtcNow.AddDays(30),
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("visa", "card-token", null, 1, "CPF", "12345678901"),
            Metadados: new Dictionary<string, string>()));

        Assert.True(response.Sucesso);
        Assert.NotNull(requestPayload);
        using var document = JsonDocument.Parse(requestPayload!);
        Assert.Equal(
            "TESTUSER978765836@testuser.com",
            document.RootElement.GetProperty("payer_email").GetString());
    }

    [Fact]
    public async Task CriarAssinaturaRecorrenteAsync_DeveUsarStartDateSemFreeTrial_QuandoTrialInternoEstiverAtivo()
    {
        string? requestPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestPayload = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id":"sub-trial","payer_id":"payer-1"}""", Encoding.UTF8, "application/json")
            };
        });

        var gateway = CriarGateway(handler, new MercadoPagoOptions
        {
            AccessToken = "TEST-123",
            ApiBaseUrl = "https://api.mercadopago.com"
        });

        var response = await gateway.CriarAssinaturaRecorrenteAsync(new CriarAssinaturaRecorrenteGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "trial-start-date",
            Descricao: "Assinatura trial",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Cliente",
            PagadorEmail: "cliente@email.com",
            DiasTrial: 30,
            PrimeiraCobrancaEm: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            PagamentoTransparente: new PagamentoTransparenteGatewayRequest("visa", "card-token", null, 1, "CPF", "12345678901"),
            Metadados: new Dictionary<string, string>()));

        Assert.True(response.Sucesso);
        Assert.NotNull(requestPayload);
        using var document = JsonDocument.Parse(requestPayload!);
        var autoRecurring = document.RootElement.GetProperty("auto_recurring");
        Assert.False(autoRecurring.TryGetProperty("free_trial", out _));
        Assert.Equal("2026-07-10T12:00:00.000Z", autoRecurring.GetProperty("start_date").GetString());
        Assert.Equal("2036-07-10T12:00:00.000Z", autoRecurring.GetProperty("end_date").GetString());
    }

    private static GatewayPagamentoMercadoPago CriarGateway(
        HttpMessageHandler handler,
        MercadoPagoOptions options)
    {
        var apiBaseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl)
            ? "https://api.mercadopago.com"
            : options.ApiBaseUrl.Trim();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/")
        };

        return new GatewayPagamentoMercadoPago(httpClient, Options.Create(options));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request));
    }
}
