using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class UsuarioNegocioContextoServiceTests
{
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IMatrizPermissaoNegocioService> _matrizPermissaoNegocioService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    [Fact]
    public async Task ListarEstabelecimentosAsync_DeveRetornarContextosDoUsuarioComPermissoesEModulos()
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 20,
            PublicGuid = Guid.NewGuid(),
            Nome = "Studio Glow",
            Logo = "logo.png",
            Ativo = true
        };

        _currentUserContext.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUserContext.SetupGet(c => c.UserId).Returns(10);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ListarAtivosPorUsuarioAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EstabelecimentoUsuario
                {
                    EstabelecimentoId = 20,
                    UsuarioId = 10,
                    RoleNoEstabelecimento = EstablishmentUserRole.Profissional,
                    Ativo = true,
                    Estabelecimento = estabelecimento
                }
            ]);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ExisteAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matrizPermissaoNegocioService
            .Setup(s => s.ObterPermissoes(
                EstablishmentUserRole.Profissional,
                true,
                false))
            .Returns(new HashSet<PermissaoNegocio>
            {
                PermissaoNegocio.AgendaVisualizarPropria,
                PermissaoNegocio.AtendimentoIniciar
            });
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModulosAssinaturaResponseDto.Liberado(
                new Assinatura
                {
                    Id = 30,
                    EstabelecimentoId = 20,
                    PlanoId = 40,
                    Status = AssinaturaStatus.Ativa,
                    Plano = new Plano { Id = 40, Nome = "Plus" }
                },
                [ModuloAssinatura.Agenda, ModuloAssinatura.Profissionais]));

        var service = CreateService();

        var response = await service.ListarEstabelecimentosAsync();

        Assert.Single(response);
        Assert.Equal(20, response[0].EstabelecimentoId);
        Assert.Equal(estabelecimento.PublicGuid, response[0].PublicGuid);
        Assert.Equal("Studio Glow", response[0].Nome);
        Assert.Equal("Profissional", response[0].Role);
        Assert.True(response[0].PossuiVinculoProfissional);
        Assert.Contains(nameof(PermissaoNegocio.AgendaVisualizarPropria), response[0].Permissoes);
        Assert.True(response[0].AssinaturaAtiva);
        Assert.Equal("Plus", response[0].PlanoNome);
        Assert.Contains(nameof(ModuloAssinatura.Profissionais), response[0].Modulos);
    }

    [Fact]
    public async Task ListarEstabelecimentosAsync_DeveLancarUnauthorized_QuandoUsuarioNaoEstaAutenticado()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.ListarEstabelecimentosAsync());
    }

    private UsuarioNegocioContextoService CreateService() =>
        new(
            _estabelecimentoUsuarioRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _matrizPermissaoNegocioService.Object,
            _modulosAssinaturaService.Object,
            _currentUserContext.Object);
}
