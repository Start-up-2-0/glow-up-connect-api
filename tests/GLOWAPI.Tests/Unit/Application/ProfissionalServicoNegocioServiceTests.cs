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
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();

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
    public async Task VincularAsync_DevePermitirPrecoEDuracaoEspecificos()
    {
        ProfissionalServico? capturado = null;
        _profissionalServicoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalServico>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalServico, CancellationToken>((vinculo, _) => capturado = vinculo);

        var service = CreateService();

        await service.VincularAsync(
            20,
            40,
            30,
            new VincularServicoProfissionalRequestDto
            {
                Preco = 95,
                DuracaoMinutos = 60
            });

        Assert.NotNull(capturado);
        Assert.Equal(95, capturado!.Preco);
        Assert.Equal(60, capturado.DuracaoMinutos);
    }

    [Fact]
    public async Task VincularAsync_DeveReativarVinculoInativo()
    {
        var existente = new ProfissionalServico
        {
            Id = 70,
            ProfissionalId = 40,
            ServicoId = 30,
            Preco = 70,
            DuracaoMinutos = 30,
            Ativo = false
        };
        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var service = CreateService();

        var response = await service.VincularAsync(
            20,
            40,
            30,
            new VincularServicoProfissionalRequestDto
            {
                Preco = 100,
                DuracaoMinutos = 50
            });

        Assert.True(existente.Ativo);
        Assert.Equal(100, existente.Preco);
        Assert.Equal(50, existente.DuracaoMinutos);
        Assert.NotNull(existente.UpdatedAt);
        Assert.Equal(70, response.Id);
        _profissionalServicoRepository.Verify(r => r.Atualizar(existente), Times.Once);
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoVinculoAtivoJaExiste()
    {
        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalServico
            {
                ProfissionalId = 40,
                ServicoId = 30,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalServicoDuplicadoException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto()));
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoServicoNaoPertenceAoNegocio()
    {
        _servicoRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 30,
                EstabelecimentoId = 99,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ServicoNegocioNaoEncontradoException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto()));
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoProfissionalNaoEstaAtivoNoNegocio()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfissionalEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalSemVinculoNegocioException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto()));
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoDadosSaoInvalidos()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalServicoInvalidoException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto
            {
                Preco = -1
            }));

        await Assert.ThrowsAsync<ProfissionalServicoInvalidoException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto
            {
                DuracaoMinutos = 0
            }));
    }

    [Fact]
    public async Task VincularAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ServicoGerenciar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.VincularAsync(20, 40, 30, new VincularServicoProfissionalRequestDto()));

        _servicoRepository.Verify(r => r.ObterPorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private ProfissionalServicoNegocioService CreateService() =>
        new(
            _servicoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _profissionalServicoRepository.Object,
            _autorizacaoNegocioService.Object);
}
