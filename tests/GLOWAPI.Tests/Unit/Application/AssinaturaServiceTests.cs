using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AssinaturaServiceTests
{
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IPlanoRepository> _planoRepository = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public AssinaturaServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
    }

    [Fact]
    public async Task IniciarAsync_DeveCriarAssinaturaPendente_ParaEstabelecimentoValido()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _assinaturaRepository
            .Setup(r => r.ExisteAtivaOuPendentePorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assinatura? assinaturaCriada = null;
        _assinaturaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()))
            .Callback<Assinatura, CancellationToken>((assinatura, _) =>
            {
                assinatura.Id = 30;
                assinaturaCriada = assinatura;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.IniciarAsync(new IniciarAssinaturaRequestDto
        {
            PlanoId = 1,
            TipoAssinatura = TipoAssinatura.Estabelecimento,
            EstabelecimentoId = 20
        });

        Assert.Equal(30, response.Id);
        Assert.Equal(1, response.PlanoId);
        Assert.Equal(20, response.EstabelecimentoId);
        Assert.Null(response.ProfissionalAutonomoId);
        Assert.Equal("PendentePagamento", response.Status);
        Assert.Equal("MercadoPago", response.Gateway);
        Assert.NotNull(assinaturaCriada);
        Assert.Equal(AssinaturaStatus.PendentePagamento, assinaturaCriada!.Status);

        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoPlanoNaoExisteOuInativo()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = false });

        var service = CreateService();

        await Assert.ThrowsAsync<PlanoNaoEncontradoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                EstabelecimentoId = 20
            }));
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoTitularNaoCorrespondeAoTipo()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<AssinaturaTitularInvalidoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                ProfissionalAutonomoId = 20
            }));
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoJaExisteAssinaturaParaEstabelecimento()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _assinaturaRepository
            .Setup(r => r.ExisteAtivaOuPendentePorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<AssinaturaDuplicadaException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                EstabelecimentoId = 20
            }));
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoProfissionalAutonomoPertenceAOutroUsuario()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 5,
                UsuarioId = 99,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
                ProfissionalAutonomoId = 5
            }));
    }

    private AssinaturaService CreateService() =>
        new(
            _assinaturaRepository.Object,
            _planoRepository.Object,
            _estabelecimentoRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _profissionalRepository.Object,
            _currentUser.Object);
}
