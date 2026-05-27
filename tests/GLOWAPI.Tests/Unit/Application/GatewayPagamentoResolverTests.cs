using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class GatewayPagamentoResolverTests
{
    [Fact]
    public void Resolver_DeveRetornarGatewayCompativel()
    {
        var mercadoPago = CriarGateway(GatewayPagamento.MercadoPago);
        var abacatePay = CriarGateway(GatewayPagamento.AbacatePay);
        var resolver = new GatewayPagamentoResolver([mercadoPago.Object, abacatePay.Object]);

        var resultado = resolver.Resolver(GatewayPagamento.AbacatePay);

        Assert.Same(abacatePay.Object, resultado);
    }

    [Fact]
    public void Resolver_DeveLancarExcecao_QuandoGatewayNaoEstiverConfigurado()
    {
        var mercadoPago = CriarGateway(GatewayPagamento.MercadoPago);
        var resolver = new GatewayPagamentoResolver([mercadoPago.Object]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.Resolver(GatewayPagamento.AbacatePay));

        Assert.Contains("AbacatePay", exception.Message);
    }

    private static Mock<IGatewayPagamento> CriarGateway(GatewayPagamento gateway)
    {
        var mock = new Mock<IGatewayPagamento>();
        mock.Setup(g => g.GatewaySuportado).Returns(gateway);
        mock
            .Setup(g => g.CriarCobrancaAsync(
                It.IsAny<CriarCobrancaGatewayRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarCobrancaGatewayResponse.Falha("{}", "{}", "fake"));

        return mock;
    }
}
