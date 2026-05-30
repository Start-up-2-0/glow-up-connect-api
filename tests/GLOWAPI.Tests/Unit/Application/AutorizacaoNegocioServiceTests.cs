using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AutorizacaoNegocioServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly MatrizPermissaoNegocioService _matrizPermissaoNegocioService = new();

    public AutorizacaoNegocioServiceTests()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserContext.Setup(c => c.UserId).Returns(10);
    }

    [Fact]
    public async Task AutorizarAsync_DeveRetornarContexto_QuandoUsuarioPossuiPermissao()
    {
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                Ativo = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ExisteAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var contexto = await service.AutorizarAsync(20, PermissaoNegocio.CaixaGerenciar);

        Assert.Equal(20, contexto.EstabelecimentoId);
        Assert.Equal(10, contexto.UsuarioId);
        Assert.Equal(EstablishmentUserRole.Owner, contexto.Role);
        Assert.False(contexto.PossuiVinculoProfissional);
        Assert.Contains(PermissaoNegocio.CaixaGerenciar, contexto.Permissoes);
    }

    [Fact]
    public async Task AutorizarAsync_DeveLancarExcecao_QuandoUsuarioNaoPossuiVinculoAtivo()
    {
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemVinculoNegocioException>(() =>
            service.AutorizarAsync(20, PermissaoNegocio.AgendaVisualizarGeral));
    }

    [Fact]
    public async Task AutorizarAsync_DeveLancarExcecao_QuandoUsuarioNaoPossuiPermissao()
    {
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.AutorizarAsync(20, PermissaoNegocio.CaixaVisualizar));
    }

    [Fact]
    public async Task ObterContextoPorPublicGuidAsync_DeveResolverNegocioEPermissoes()
    {
        var publicGuid = Guid.NewGuid();

        _estabelecimentoRepository
            .Setup(r => r.ObterPorPublicGuidAsync(publicGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, PublicGuid = publicGuid, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Admin,
                Ativo = true
            });

        var service = CreateService();

        var contexto = await service.ObterContextoPorPublicGuidAsync(publicGuid);

        Assert.Equal(20, contexto.EstabelecimentoId);
        Assert.Equal(EstablishmentUserRole.Admin, contexto.Role);
        Assert.Contains(PermissaoNegocio.EquipeGerenciar, contexto.Permissoes);
    }

    [Fact]
    public async Task ObterContextoPorPublicGuidAsync_DeveLancarExcecao_QuandoNegocioNaoExistir()
    {
        var publicGuid = Guid.NewGuid();

        _estabelecimentoRepository
            .Setup(r => r.ObterPorPublicGuidAsync(publicGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Estabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NegocioNaoEncontradoException>(() =>
            service.ObterContextoPorPublicGuidAsync(publicGuid));
    }

    [Fact]
    public async Task ObterContextoAsync_DeveUnirPermissoes_QuandoUsuarioTambemTemVinculoProfissional()
    {
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
                Ativo = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ExisteAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var contexto = await service.ObterContextoAsync(20);

        Assert.True(contexto.PossuiVinculoProfissional);
        Assert.Contains(PermissaoNegocio.AgendaVisualizarGeral, contexto.Permissoes);
        Assert.Contains(PermissaoNegocio.AgendaVisualizarPropria, contexto.Permissoes);
        Assert.Contains(PermissaoNegocio.AtendimentoIniciar, contexto.Permissoes);
        Assert.DoesNotContain(PermissaoNegocio.CaixaVisualizar, contexto.Permissoes);
    }

    [Fact]
    public async Task PossuiPermissaoAsync_DeveRetornarBooleanoDaPermissao()
    {
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Manager,
                Ativo = true
            });

        var service = CreateService();

        var possui = await service.PossuiPermissaoAsync(20, PermissaoNegocio.ProfissionalGerenciar);
        var naoPossui = await service.PossuiPermissaoAsync(20, PermissaoNegocio.CaixaVisualizar);

        Assert.True(possui);
        Assert.False(naoPossui);
    }

    [Fact]
    public async Task ObterContextoAsync_DeveLancarUnauthorized_QuandoUsuarioNaoEstiverAutenticado()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(false);
        _currentUserContext.Setup(c => c.UserId).Returns((int?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.ObterContextoAsync(20));
    }

    private AutorizacaoNegocioService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _matrizPermissaoNegocioService,
            _currentUserContext.Object);
}
