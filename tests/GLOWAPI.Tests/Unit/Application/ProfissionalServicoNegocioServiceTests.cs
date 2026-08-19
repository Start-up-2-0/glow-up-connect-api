using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ProfissionalServicoNegocioServiceTests
{
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IProfissionalServicoRepository> _profissionalServicoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
    private readonly Mock<IOnboardingPublicacaoService> _onboardingPublicacaoService = new();

    public ProfissionalServicoNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ServicoGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ServicoGerenciar }));

        _servicoRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 30,
                EstabelecimentoId = 20,
                Nome = "Corte",
                PrecoBase = 80,
                DuracaoMinutos = 45,
                Ativo = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                Id = 50,
                ProfissionalId = 40,
                EstabelecimentoId = 20,
                Ativo = true
            });

        _agendamentoItemRepository
            .Setup(r => r.ExisteFuturoConfirmadoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task VincularAsync_DeveCriarVinculoComDadosPadraoDoServico()
    {
        ProfissionalServico? capturado = null;
        _profissionalServicoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalServico>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalServico, CancellationToken>((vinculo, _) => capturado = vinculo);

        var service = CreateService();

        var response = await service.VincularAsync(
            20,
            40,
            30,
            new VincularServicoProfissionalRequestDto());

        Assert.NotNull(capturado);
        Assert.Equal(40, capturado!.ProfissionalId);
        Assert.Equal(30, capturado.ServicoId);
        Assert.Equal(80, capturado.Preco);
        Assert.Equal(45, capturado.DuracaoMinutos);
        Assert.True(capturado.Ativo);
        Assert.Equal(80, response.Preco);
        Assert.Equal(45, response.DuracaoMinutos);
        _profissionalServicoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarPrecoEDuracao()
    {
        var vinculo = new ProfissionalServico
        {
            Id = 70,
            ProfissionalId = 40,
            ServicoId = 30,
            Preco = 80,
            DuracaoMinutos = 45,
            Ativo = true
        };

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var service = CreateService();
        var response = await service.AtualizarAsync(
            20,
            40,
            30,
            new AtualizarProfissionalServicoRequestDto
            {
                Preco = 95,
                DuracaoMinutos = 60
            });

        Assert.Equal(95, vinculo.Preco);
        Assert.Equal(60, vinculo.DuracaoMinutos);
        Assert.Equal(95, response.Preco);
    }

    [Fact]
    public async Task DesvincularAsync_DeveInativarVinculo()
    {
        var vinculo = new ProfissionalServico
        {
            Id = 70,
            ProfissionalId = 40,
            ServicoId = 30,
            Preco = 80,
            DuracaoMinutos = 45,
            Ativo = true
        };

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var service = CreateService();
        var response = await service.DesvincularAsync(20, 40, 30);

        Assert.False(vinculo.Ativo);
        Assert.False(response.Ativo);
    }

    [Fact]
    public async Task DesvincularAsync_DeveLancarExcecao_QuandoExistirAgendamentoFuturo()
    {
        var vinculo = new ProfissionalServico
        {
            Id = 70,
            ProfissionalId = 40,
            ServicoId = 30,
            Ativo = true
        };

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        _agendamentoItemRepository
            .Setup(r => r.ExisteFuturoConfirmadoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalServicoComAgendamentoFuturoException>(() =>
            service.DesvincularAsync(20, 40, 30));
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoDadosSaoInvalidos()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ServicoNegocioInvalidoException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto
            {
                Preco = -1
            }));
    }

    private ProfissionalServicoNegocioService CreateService() =>
        new(
            _servicoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _profissionalServicoRepository.Object,
            _agendamentoItemRepository.Object,
            _autorizacaoNegocioService.Object,
            _auditoriaNegocioService.Object,
            _onboardingPublicacaoService.Object);
}
