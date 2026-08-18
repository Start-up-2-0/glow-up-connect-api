using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ExclusaoContaServiceTests
{
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuthSessionService> _authSessionService = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IAssinaturaVisibilidadeService> _assinaturaVisibilidadeService = new();
    private readonly Mock<IAssinaturaEncerramentoService> _assinaturaEncerramentoService = new();
    private readonly Mock<IAssinaturaHistoricoService> _assinaturaHistoricoService = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IAgendamentoRepository> _agendamentoRepository = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemNotificacaoService = new();

    public ExclusaoContaServiceTests()
    {
        _currentUser.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(10);
        _passwordHasher.Setup(h => h.Verify("Senha123!", It.IsAny<string>())).Returns(true);
        _agendamentoRepository
            .Setup(r => r.ListarPorUsuarioClienteAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Agendamento>());
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MensagemNotificacaoResponseDto());
    }

    [Fact]
    public async Task SolicitarAsync_Owner_DeveSuspenderAssinaturaOcultarLojasERevogarSessoes()
    {
        var usuario = CriarUsuario();
        var assinatura = new Assinatura
        {
            Id = 5,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Ativa,
            RenovacaoAutomatica = true
        };

        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ListarAtivosPorUsuarioAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EstabelecimentoUsuario
                {
                    EstabelecimentoId = 20,
                    UsuarioId = 10,
                    RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                    Ativo = true
                }
            ]);
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var service = CreateService();
        await service.SolicitarAsync("Senha123!");

        Assert.Equal(ExclusaoStatus.Pendente, usuario.ExclusaoStatus);
        Assert.NotNull(usuario.ExclusaoEfetivarEm);
        Assert.Equal(AssinaturaStatus.Suspensa, assinatura.Status);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.StatusAntesExclusao);
        Assert.False(assinatura.RenovacaoAutomatica);
        _assinaturaVisibilidadeService.Verify(
            s => s.OcultarLojasVinculadasAsync(assinatura, It.IsAny<CancellationToken>()),
            Times.Once);
        _authSessionService.Verify(
            s => s.RevogarTodasSessoesDoUsuarioAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);
        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SolicitarAsync_ProfissionalDaEquipe_NaoDeveAlterarAssinatura()
    {
        var usuario = CriarUsuario();
        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ListarAtivosPorUsuarioAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EstabelecimentoUsuario
                {
                    EstabelecimentoId = 20,
                    UsuarioId = 10,
                    RoleNoEstabelecimento = EstablishmentUserRole.Profissional,
                    Ativo = true
                }
            ]);

        var service = CreateService();
        await service.SolicitarAsync("Senha123!");

        Assert.Equal(ExclusaoStatus.Pendente, usuario.ExclusaoStatus);
        _assinaturaRepository.Verify(
            r => r.ObterAtualPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _assinaturaVisibilidadeService.Verify(
            s => s.OcultarLojasVinculadasAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReativarAsync_DentroDoPrazo_DeveRestaurarAssinaturaELogin()
    {
        var usuario = CriarUsuario();
        usuario.ExclusaoStatus = ExclusaoStatus.Pendente;
        usuario.ExclusaoSolicitadaEm = DateTime.UtcNow.AddDays(-1);
        usuario.ExclusaoEfetivarEm = DateTime.UtcNow.AddDays(29);
        var assinatura = new Assinatura
        {
            Id = 5,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Suspensa,
            StatusAntesExclusao = AssinaturaStatus.Trial,
            RenovacaoAutomatica = false
        };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("owner@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ListarAtivosPorUsuarioAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EstabelecimentoUsuario
                {
                    EstabelecimentoId = 20,
                    UsuarioId = 10,
                    RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                    Ativo = true
                }
            ]);
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<AuthSessionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthLoginResult("t", "r", DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow.AddDays(7), new UsuarioAuthInfo(10, "Owner", "owner@email.com", UserRole.DonoEstabelecimento)));

        var service = CreateService();
        var resultado = await service.ReativarAsync("owner@email.com", "Senha123!", new AuthSessionContext("127.0.0.1", "test"));

        Assert.Equal(ExclusaoStatus.Nenhuma, usuario.ExclusaoStatus);
        Assert.Null(usuario.ExclusaoEfetivarEm);
        Assert.Equal(AssinaturaStatus.Trial, assinatura.Status);
        Assert.Null(assinatura.StatusAntesExclusao);
        Assert.True(assinatura.RenovacaoAutomatica);
        Assert.Equal("t", resultado.Token);
        _assinaturaVisibilidadeService.Verify(
            s => s.ReexibirLojasVinculadasAsync(assinatura, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EfetivarVencidasAsync_DeveEncerrarAnonimizarEDesativarLoja()
    {
        var usuario = CriarUsuario();
        usuario.ExclusaoStatus = ExclusaoStatus.Pendente;
        usuario.ExclusaoEfetivarEm = DateTime.UtcNow.AddDays(-1);
        var assinatura = new Assinatura { Id = 5, EstabelecimentoId = 20, Status = AssinaturaStatus.Suspensa };
        var estabelecimento = new Estabelecimento { Id = 20, Ativo = true, VisivelPublicamente = false };

        _usuarioRepository
            .Setup(r => r.ListarExclusoesPendentesVencidasAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([usuario]);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ListarAtivosPorUsuarioAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new EstabelecimentoUsuario
                {
                    EstabelecimentoId = 20,
                    UsuarioId = 10,
                    RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                    Ativo = true
                }
            ]);
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);

        var service = CreateService();
        var efetivadas = await service.EfetivarVencidasAsync();

        Assert.Equal(1, efetivadas);
        Assert.Equal(ExclusaoStatus.Concluida, usuario.ExclusaoStatus);
        Assert.False(usuario.Ativo);
        Assert.Equal("Titular removido", usuario.Nome);
        Assert.Equal("deleted+10@invalid.local", usuario.Email);
        Assert.False(estabelecimento.Ativo);
        _assinaturaEncerramentoService.Verify(
            s => s.EncerrarAsync(
                assinatura,
                AssinaturaStatus.Cancelada,
                "AssinaturaEncerradaExclusaoConta",
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReativarAsync_ForaDoPrazo_DeveLancarInactiveUser()
    {
        var usuario = CriarUsuario();
        usuario.ExclusaoStatus = ExclusaoStatus.Pendente;
        usuario.ExclusaoEfetivarEm = DateTime.UtcNow.AddDays(-1);
        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("owner@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        await Assert.ThrowsAsync<InactiveUserException>(() =>
            service.ReativarAsync("owner@email.com", "Senha123!", new AuthSessionContext(null, null)));
    }

    private static Usuario CriarUsuario() =>
        new()
        {
            Id = 10,
            Nome = "Owner",
            Email = "owner@email.com",
            Senha = "hash",
            Ativo = true,
            Role = UserRole.DonoEstabelecimento
        };

    private ExclusaoContaService CreateService() =>
        new(
            _currentUser.Object,
            _usuarioRepository.Object,
            _passwordHasher.Object,
            _authSessionService.Object,
            _authService.Object,
            _estabelecimentoUsuarioRepository.Object,
            _assinaturaRepository.Object,
            _assinaturaVisibilidadeService.Object,
            _assinaturaEncerramentoService.Object,
            _assinaturaHistoricoService.Object,
            _estabelecimentoRepository.Object,
            _agendamentoRepository.Object,
            _mensagemNotificacaoService.Object,
            Options.Create(new ExclusaoContaOptions { DiasCarencia = 30 }),
            Options.Create(new AuthOptions { FrontendBaseUrl = "http://localhost:5173" }));
}
