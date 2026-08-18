using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class MovimentacaoCaixaServiceTests
{
    private readonly Mock<ICaixaRepository> _caixaRepository = new();
    private readonly Mock<ILancamentoCaixaRepository> _lancamentoCaixaRepository = new();
    private readonly Mock<ISessaoCaixaRepository> _sessaoCaixaRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    public MovimentacaoCaixaServiceTests()
    {
        _currentUserContext.Setup(c => c.UserId).Returns(1);
    }

    [Fact]
    public async Task RegistrarLancamentoAsync_DeveCriarLancamentoERecalcularSaldo()
    {
        var caixa = new Caixa
        {
            Id = 1,
            EstabelecimentoId = 20,
            SaldoTotal = 0,
            SaldoDisponivel = 0
        };

        _caixaRepository
            .Setup(r => r.ObterOuProvisionarPorEstabelecimentoComTrackingAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caixa);

        _caixaRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caixa);

        _lancamentoCaixaRepository
            .Setup(r => r.ListarTodosPorCaixaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LancamentoCaixa>
            {
                new()
                {
                    Id = 10,
                    CaixaId = 1,
                    Tipo = LancamentoCaixaTipo.AjusteManual,
                    Valor = 100,
                    CreateAd = DateTime.UtcNow
                }
            });

        var service = CreateService();
        var lancamento = await service.RegistrarLancamentoAsync(
            20,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.AjusteManual,
                100,
                "Reforco teste"));

        Assert.Equal(LancamentoCaixaTipo.AjusteManual, lancamento.Tipo);
        Assert.Equal(100, caixa.SaldoTotal);
        _lancamentoCaixaRepository.Verify(r => r.AdicionarAsync(
            It.IsAny<LancamentoCaixa>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarLancamentoAsync_DeveProvisionarCaixa_QuandoNaoExistir()
    {
        var caixa = new Caixa
        {
            Id = 1,
            EstabelecimentoId = 20,
            SaldoTotal = 0,
            SaldoDisponivel = 0
        };

        _caixaRepository
            .Setup(r => r.ObterOuProvisionarPorEstabelecimentoComTrackingAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caixa);

        _caixaRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caixa);

        _lancamentoCaixaRepository
            .Setup(r => r.ListarTodosPorCaixaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LancamentoCaixa>
            {
                new()
                {
                    Id = 10,
                    CaixaId = 1,
                    Tipo = LancamentoCaixaTipo.Saque,
                    Valor = 500,
                    CreateAd = DateTime.UtcNow
                }
            });

        var service = CreateService();
        var lancamento = await service.RegistrarLancamentoAsync(
            20,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.Saque,
                500,
                "Pagamento Aluguel"));

        Assert.Equal(LancamentoCaixaTipo.Saque, lancamento.Tipo);
        Assert.Equal(-500, caixa.SaldoTotal);
    }

    [Fact]
    public async Task RegistrarLancamentoAsync_DeveLancarExcecao_QuandoValorInvalido()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<LancamentoCaixaInvalidoException>(() =>
            service.RegistrarLancamentoAsync(
                20,
                new RegistrarLancamentoCaixaComando(
                    LancamentoCaixaTipo.AjusteManual,
                    0,
                    "Teste")));
    }

    private MovimentacaoCaixaService CreateService() =>
        new(
            _caixaRepository.Object,
            _lancamentoCaixaRepository.Object,
            _sessaoCaixaRepository.Object,
            _currentUserContext.Object);
}
