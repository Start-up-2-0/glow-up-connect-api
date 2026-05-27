using System.Text.Json;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Pagamentos;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class GatewayPagamentoFakeTests
{
    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarRespostaPadronizada()
    {
        var gateway = new GatewayPagamentoFake(GatewayPagamento.MercadoPago);

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.MercadoPago,
            ReferenciaInterna: "assinatura-1",
            Descricao: "Assinatura Plano Pro",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com",
            Metadados: new Dictionary<string, string>
            {
                ["assinaturaId"] = "1"
            }));

        Assert.True(response.Sucesso);
        Assert.StartsWith("mercadopago-", response.GatewayPaymentId);
        Assert.Contains(response.GatewayPaymentId, response.CheckoutUrl);
        Assert.False(string.IsNullOrWhiteSpace(response.RequestPayload));
        Assert.False(string.IsNullOrWhiteSpace(response.ResponsePayload));
        Assert.Null(response.MensagemErro);

        using var json = JsonDocument.Parse(response.RequestPayload);
        Assert.Equal("MercadoPago", json.RootElement.GetProperty("gateway").GetString());
        Assert.Equal("assinatura-1", json.RootElement.GetProperty("reference").GetString());
        Assert.Equal("maria@email.com", json.RootElement.GetProperty("payer").GetProperty("email").GetString());
    }

    [Fact]
    public async Task CriarCobrancaAsync_DeveRetornarFalha_QuandoGatewayForIncompativel()
    {
        var gateway = new GatewayPagamentoFake(GatewayPagamento.MercadoPago);

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: GatewayPagamento.AbacatePay,
            ReferenciaInterna: "assinatura-1",
            Descricao: "Assinatura Plano Pro",
            Valor: 99.90m,
            Moeda: "BRL",
            PagadorNome: "Maria",
            PagadorEmail: "maria@email.com"));

        Assert.False(response.Sucesso);
        Assert.Equal(string.Empty, response.GatewayPaymentId);
        Assert.Contains("Gateway incompativel", response.MensagemErro);
    }
}
