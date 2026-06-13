using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ServicoNegocioServiceTests
{
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IProfissionalEscopoAcessoService> _profissionalEscopoAcessoService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();

    public ServicoNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ServicoGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAutorizacao(PermissaoNegocio.ServicoGerenciar));

        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ServicoVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarAutorizacao(PermissaoNegocio.ServicoVisualizar));

        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModulosAssinaturaResponseDto.Liberado(
                new Assinatura { Id = 1, Plano = new Plano { LimiteServicos = 10 } },
                20,
                [ModuloAssinatura.Servicos]));

        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    [Fact]
    public async Task CriarAsync_DeveCriarServicoAtivo()
    {
        Servico? capturado = null;
        _servicoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Servico>(), It.IsAny<CancellationToken>()))
            .Callback<Servico, CancellationToken>((servico, _) => capturado = servico)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var response = await service.CriarAsync(
            20,
            new CriarServicoRequestDto
            {
                Nome = "Corte",
                Descricao = "Corte masculino",
                PrecoBase = 80,
                DuracaoMinutos = 45
            });

        Assert.NotNull(capturado);
        Assert.Equal(20, capturado!.EstabelecimentoId);
        Assert.True(capturado.Ativo);
        Assert.Equal("Corte", response.Nome);
        _auditoriaNegocioService.Verify(
            s => s.RegistrarAsync(
                20,
                TipoAcaoAuditoriaNegocio.ServicoCriado,
                nameof(Servico),
                It.IsAny<int?>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoLimiteAtingido()
    {
        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteServicosNegocioExcedidoException>(() =>
            service.CriarAsync(
                20,
                new CriarServicoRequestDto
                {
                    Nome = "Corte",
                    PrecoBase = 80,
                    DuracaoMinutos = 45
                }));
    }

    [Fact]
    public async Task ListarAsync_DeveAplicarFiltros()
    {
        _servicoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                20,
                true,
                40,
                "Corte",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Servico
                {
                    Id = 30,
                    EstabelecimentoId = 20,
                    Nome = "Corte",
                    PrecoBase = 80,
                    DuracaoMinutos = 45,
                    Ativo = true
                }
            ]);

        var service = CreateService();
        var response = await service.ListarAsync(
            20,
            new ServicoFiltroDto
            {
                Ativo = true,
                ProfissionalId = 40,
                Nome = "Corte"
            });

        Assert.Single(response);
        Assert.Equal(30, response[0].Id);
    }

    [Fact]
    public async Task ListarAsync_DeveRestringirAosVinculosDoProfissional()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ServicoVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ServicoVisualizar }));

        _profissionalEscopoAcessoService
            .Setup(s => s.ObterEscopoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EscopoProfissionalResultado(20, 10, 40, 1, true));

        _servicoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                20,
                null,
                40,
                null,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Servico
                {
                    Id = 31,
                    EstabelecimentoId = 20,
                    Nome = "Barba",
                    PrecoBase = 50,
                    DuracaoMinutos = 30,
                    Ativo = true
                }
            ]);

        var service = CreateService();
        var response = await service.ListarAsync(20, new ServicoFiltroDto());

        Assert.Single(response);
        Assert.Equal(31, response[0].Id);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveValidarLimiteAoReativar()
    {
        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(30, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 30,
                EstabelecimentoId = 20,
                Nome = "Corte",
                Ativo = false,
                PrecoBase = 80,
                DuracaoMinutos = 45
            });

        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteServicosNegocioExcedidoException>(() =>
            service.AtualizarStatusAsync(
                20,
                30,
                new AtualizarStatusServicoRequestDto { Ativo = true }));
    }

    private ServicoNegocioService CreateService() => new(
        _servicoRepository.Object,
        _estabelecimentoRepository.Object,
        _profissionalRepository.Object,
        _profissionalEstabelecimentoRepository.Object,
        _autorizacaoNegocioService.Object,
        _profissionalEscopoAcessoService.Object,
        _modulosAssinaturaService.Object,
        _auditoriaNegocioService.Object);

    private static AutorizacaoNegocioResultado CriarAutorizacao(PermissaoNegocio permissao) =>
        new(20, 10, EstablishmentUserRole.Owner, false, new HashSet<PermissaoNegocio> { permissao });
}
