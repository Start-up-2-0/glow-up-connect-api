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

    [Fact]
    public async Task ObterDashboardAsync_SemCaixa_RetornaDashboardZerado()
    {
        _caixaRepository
            .Setup(r => r.ObterPorEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Caixa?)null);
        _contaReceberRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ContaReceber>());
        _contaPagarRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ContaPagar>());

        var service = CreateService();
        var inicio = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);

        var resultado = await service.ObterDashboardAsync(
            1,
            new FinanceiroFiltroDto(inicio, fim),
            CancellationToken.None);

        Assert.Equal(0, resultado.SaldoAtual);
        Assert.Equal(0, resultado.TotalEntradas);
        Assert.Equal(0, resultado.TotalSaidas);
        Assert.Equal(0, resultado.LucroLiquido);
        Assert.Equal(0, resultado.ContasEmAberto);
        Assert.Equal(0, resultado.QuantidadeContasEmAberto);
        Assert.Equal(0, resultado.ComissoesPeriodo);
        Assert.Equal(inicio, resultado.PeriodoInicio);
        Assert.Equal(fim, resultado.PeriodoFim);
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
