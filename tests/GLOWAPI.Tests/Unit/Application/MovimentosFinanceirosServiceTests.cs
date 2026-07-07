using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class MovimentosFinanceirosServiceTests
{
    private readonly Mock<ICaixaRepository> _caixaRepository = new();
    private readonly Mock<ILancamentoCaixaRepository> _lancamentoCaixaRepository = new();
    private readonly Mock<IContaReceberRepository> _contaReceberRepository = new();
    private readonly Mock<IContaPagarRepository> _contaPagarRepository = new();
    private readonly Mock<IFinanceiroNegocioService> _financeiroNegocioService = new();
    private readonly Mock<IMovimentacaoCaixaService> _movimentacaoCaixaService = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();

    public MovimentosFinanceirosServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(It.IsAny<int>(), It.IsAny<PermissaoNegocio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                1,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.CaixaGerenciar }));
    }

    [Fact]
    public async Task CriarEntradaAsync_ComVencimentoFuturo_CriaContaReceber()
    {
        _financeiroNegocioService
            .Setup(s => s.CriarContaReceberAsync(1, It.IsAny<CriarContaReceberRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaReceberResponseDto(
                5,
                1,
                null,
                "Servico",
                200,
                DateTime.UtcNow.AddDays(10),
                "Aberta"));

        var service = CreateService();
        var resultado = await service.CriarEntradaAsync(
            1,
            new CriarMovimentoFinanceiroRequestDto(
                200,
                "Servico",
                null,
                null,
                null,
                DateTime.UtcNow.AddDays(10)));

        Assert.Equal("conta:5", resultado.Id);
        Assert.Equal("pendente", resultado.Status);
        _movimentacaoCaixaService.Verify(
            s => s.RegistrarLancamentoAsync(It.IsAny<int>(), It.IsAny<RegistrarLancamentoCaixaComando>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CriarEntradaAsync_Imediata_RegistraLancamento()
    {
        _movimentacaoCaixaService
            .Setup(s => s.RegistrarLancamentoAsync(1, It.IsAny<RegistrarLancamentoCaixaComando>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LancamentoCaixa
            {
                Id = 9,
                Tipo = LancamentoCaixaTipo.AjusteManual,
                Valor = 50,
                Descricao = "Venda balcao",
                CreateAd = DateTime.UtcNow
            });

        var service = CreateService();
        var resultado = await service.CriarEntradaAsync(
            1,
            new CriarMovimentoFinanceiroRequestDto(50, "Venda balcao", DateTime.UtcNow, "Pix", null, null));

        Assert.Equal("lancamento:9", resultado.Id);
        Assert.Equal("recebido", resultado.Status);
    }

    private MovimentosFinanceirosService CreateService() =>
        new(
            _caixaRepository.Object,
            _lancamentoCaixaRepository.Object,
            _contaReceberRepository.Object,
            _contaPagarRepository.Object,
            _financeiroNegocioService.Object,
            _movimentacaoCaixaService.Object,
            _autorizacaoNegocioService.Object);
}
