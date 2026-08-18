using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class CaixaNegocioServiceTests
{
    private readonly Mock<ICaixaRepository> _caixaRepository = new();
    private readonly Mock<ILancamentoCaixaRepository> _lancamentoCaixaRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();

    public CaixaNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.CaixaVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.CaixaVisualizar }));
    }

    [Fact]
    public async Task ObterResumoAsync_DeveAutorizarERetornarSaldos()
    {
        _caixaRepository
            .Setup(r => r.ObterOuProvisionarPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Caixa
            {
                Id = 30,
                EstabelecimentoId = 20,
                SaldoTotal = 1000,
                SaldoDisponivel = 800,
                SaldoRetido = 200
            });

        var service = CreateService();

        var response = await service.ObterResumoAsync(20);

        Assert.Equal(30, response.Id);
        Assert.Equal(20, response.EstabelecimentoId);
        Assert.Equal(1000, response.SaldoTotal);
        Assert.Equal(800, response.SaldoDisponivel);
        Assert.Equal(200, response.SaldoRetido);
        _autorizacaoNegocioService.Verify(s => s.AutorizarAsync(
            20,
            PermissaoNegocio.CaixaVisualizar,
            It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.CaixaResumoConsultado,
            nameof(Caixa),
            30,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterResumoAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.CaixaVisualizar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.ObterResumoAsync(20));

        _caixaRepository.Verify(
            r => r.ObterOuProvisionarPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ObterResumoAsync_DeveProvisionarCaixa_QuandoNaoExistir()
    {
        _caixaRepository
            .Setup(r => r.ObterOuProvisionarPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Caixa
            {
                Id = 30,
                EstabelecimentoId = 20,
                SaldoTotal = 0,
                SaldoDisponivel = 0,
                SaldoRetido = 0
            });

        var service = CreateService();

        var response = await service.ObterResumoAsync(20);

        Assert.Equal(30, response.Id);
        Assert.Equal(0, response.SaldoTotal);
    }

    [Fact]
    public async Task ListarLancamentosAsync_DeveAutorizarERepassarFiltroPorCaixa()
    {
        var inicio = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        LancamentoCaixaFiltro? filtroCapturado = null;

        _caixaRepository
            .Setup(r => r.ObterOuProvisionarPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Caixa { Id = 30, EstabelecimentoId = 20 });
        _lancamentoCaixaRepository
            .Setup(r => r.ListarPorCaixaPaginadoAsync(It.IsAny<LancamentoCaixaFiltro>(), It.IsAny<CancellationToken>()))
            .Callback<LancamentoCaixaFiltro, CancellationToken>((filtro, _) => filtroCapturado = filtro)
            .ReturnsAsync((
            [
                new LancamentoCaixa
                {
                    Id = 40,
                    CaixaId = 30,
                    AgendamentoId = 50,
                    PagamentoId = 60,
                    ProfissionalId = 70,
                    Tipo = LancamentoCaixaTipo.EntradaAgendamento,
                    Valor = 150,
                    Descricao = "Pagamento aprovado",
                    CreateAd = inicio.AddDays(1)
                }
            ],
            1));

        var service = CreateService();

        var response = await service.ListarLancamentosAsync(20, new LancamentoCaixaFiltroDto
        {
            Inicio = inicio,
            Fim = fim
        });

        Assert.NotNull(filtroCapturado);
        Assert.Equal(30, filtroCapturado!.CaixaId);
        Assert.Equal(inicio, filtroCapturado.Inicio);
        Assert.Equal(fim, filtroCapturado.Fim);
        Assert.Equal(1, response.Total);
        Assert.Single(response.Itens);
        Assert.Equal("EntradaAgendamento", response.Itens[0].Tipo);
        Assert.Equal(150, response.Itens[0].Valor);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.CaixaLancamentosConsultados,
            nameof(LancamentoCaixa),
            30,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private readonly Mock<IMovimentacaoCaixaService> _movimentacaoCaixaService = new();

    private CaixaNegocioService CreateService() =>
        new(
            _caixaRepository.Object,
            _lancamentoCaixaRepository.Object,
            _movimentacaoCaixaService.Object,
            _autorizacaoNegocioService.Object,
            _auditoriaNegocioService.Object);
}
