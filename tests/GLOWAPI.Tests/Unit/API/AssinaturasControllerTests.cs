using GLOWAPI.API.Controllers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GLOWAPI.Tests.Unit.API;

public class AssinaturasControllerTests
{
    private readonly Mock<IAssinaturaService> _assinaturaService = new();
    private readonly Mock<ICobrancaAssinaturaService> _cobrancaAssinaturaService = new();
    private readonly Mock<IAssinaturaOnboardingContextoService> _assinaturaOnboardingContextoService = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    [Fact]
    public async Task TrocarPlano_DeveRetornarOkComAssinatura()
    {
        _assinaturaService
            .Setup(s => s.TrocarPlanoAsync(
                30,
                It.Is<TrocarPlanoAssinaturaRequestDto>(request => request.NovoPlanoId == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssinaturaResponseDto(
                Id: 30,
                PlanoId: 1,
                PlanoAlteracaoPendenteId: 2,
                EstabelecimentoId: 20,
                ProfissionalAutonomoId: null,
                Status: "Ativa",
                Gateway: GatewayPagamento.MercadoPago.ToString(),
                Inicio: DateTime.UtcNow,
                Fim: DateTime.UtcNow.AddMonths(1),
                PagamentoInicial: null));

        var controller = new AssinaturasController(
            _assinaturaService.Object,
            _cobrancaAssinaturaService.Object,
            _assinaturaOnboardingContextoService.Object,
            _currentUser.Object);

        var result = await controller.TrocarPlano(
            30,
            new TrocarPlanoAssinaturaRequestDto { NovoPlanoId = 2 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<AssinaturaResponseDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Solicitacao de troca de plano registrada com sucesso.", response.Message);
        Assert.Equal(30, response.Data!.Id);
        Assert.Equal(2, response.Data.PlanoAlteracaoPendenteId);

        _assinaturaService.Verify(s => s.TrocarPlanoAsync(
            30,
            It.IsAny<TrocarPlanoAssinaturaRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_DeveRetornarOkComAssinaturaCancelada()
    {
        _assinaturaService
            .Setup(s => s.CancelarAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssinaturaResponseDto(
                Id: 30,
                PlanoId: 1,
                PlanoAlteracaoPendenteId: null,
                EstabelecimentoId: 20,
                ProfissionalAutonomoId: null,
                Status: "Cancelada",
                Gateway: GatewayPagamento.MercadoPago.ToString(),
                Inicio: DateTime.UtcNow.AddDays(-10),
                Fim: DateTime.UtcNow.AddDays(20),
                PagamentoInicial: null));

        var controller = new AssinaturasController(
            _assinaturaService.Object,
            _cobrancaAssinaturaService.Object,
            _assinaturaOnboardingContextoService.Object,
            _currentUser.Object);

        var result = await controller.Cancelar(30, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<AssinaturaResponseDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Assinatura cancelada com sucesso.", response.Message);
        Assert.Equal(30, response.Data!.Id);
        Assert.Equal("Cancelada", response.Data.Status);

        _assinaturaService.Verify(s => s.CancelarAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }
}
