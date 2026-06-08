using GLOWAPI.Application.Models.Pagamentos;

namespace GLOWAPI.Tests.Unit.Application;

public class GatewayPagamentoErrorDetailsTests
{
    [Fact]
    public void FromAssinaturaRecorrente_DeveExtrairStatusHttpMensagemERespostaDoGateway()
    {
        var response = CriarAssinaturaRecorrenteGatewayResponse.Falha(
            "{}",
            """{"message":"service unavailable","status":503}""",
            "Mercado Pago retornou 503 ao criar assinatura recorrente: service unavailable");

        var details = GatewayPagamentoErrorDetails.FromAssinaturaRecorrente(response);

        Assert.Equal("criar_assinatura_recorrente", details.Operacao);
        Assert.Equal(503, details.HttpStatusCode);
        Assert.Equal("service unavailable", details.GatewayMessage);
        Assert.NotNull(details.GatewayResponse);
        Assert.NotNull(details.RequestPayload);
    }

    [Fact]
    public void FromAssinaturaRecorrente_DeveMarcarRespostaVazia_QuandoGatewayNaoRetornarCorpo()
    {
        var response = CriarAssinaturaRecorrenteGatewayResponse.Falha(
            """{"card_token_id":"abc","status":"authorized"}""",
            string.Empty,
            "Mercado Pago retornou 503 ao criar assinatura recorrente.",
            new GatewayHttpFailureInfo(
                503,
                "https://api.mercadopago.com/preapproval",
                new Dictionary<string, string> { ["x-request-id"] = "req-1" }));

        var details = GatewayPagamentoErrorDetails.FromAssinaturaRecorrente(response);

        Assert.True(details.RespostaVazia);
        Assert.Equal(string.Empty, details.GatewayResponseRaw);
        Assert.Null(details.GatewayMessage);
        Assert.Equal("https://api.mercadopago.com/preapproval", details.RequestUri);
        Assert.Equal("req-1", details.ResponseHeaders?["x-request-id"]);
    }

    [Fact]
    public void FromCobranca_DeveRetornarPayloadBruto_QuandoRespostaNaoForJson()
    {
        var response = CriarCobrancaGatewayResponse.Falha(
            "{}",
            "upstream timeout",
            "Mercado Pago retornou 503 ao criar pagamento.");

        var details = GatewayPagamentoErrorDetails.FromCobranca(response);

        Assert.Equal("criar_cobranca", details.Operacao);
        Assert.Equal(503, details.HttpStatusCode);
        Assert.Equal("upstream timeout", details.GatewayResponse);
    }
}
