using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConviteNegocioServiceTests
{
    private const string TokenUuid = "11111111-1111-1111-1111-111111111111";

    private readonly Mock<IConviteNegocioRepository> _conviteRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
    private readonly Mock<IUsuarioService> _usuarioService = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    public ConviteNegocioServiceTests()
    {
        _currentUserContext.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUserContext.SetupGet(c => c.UserId).Returns(10);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(20, It.IsAny<PermissaoNegocio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio>
                {
                    PermissaoNegocio.EquipeGerenciar,
                    PermissaoNegocio.ProfissionalConvidar,
                    PermissaoNegocio.ProfissionalGerenciar
                }));
        _tokenService.Setup(s => s.HashToken(It.IsAny<string>())).Returns("hash-token");
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModulosAssinaturaResponseDto.Liberado(
                new Assinatura
                {
                    Id = 1,
                    EstabelecimentoId = 20,
                    PlanoId = 2,
                    Status = AssinaturaStatus.Ativa,
                    Plano = new Plano { Id = 2, Nome = "Plus" }
                },
                20,
                [ModuloAssinatura.Profissionais]));
        _conviteRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _conviteRepository
            .Setup(r => r.TentarRegistrarUtilizacaoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conviteRepository
            .Setup(r => r.UsuarioJaUtilizouAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    [Fact]
    public async Task CriarLinkAsync_DeveCriarConviteAtivoComPadraoUmDiaELinkNaLanding()
    {
        ConviteNegocio? conviteCriado = null;
        string? tokenHasheado = null;
        _conviteRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ConviteNegocio>(), It.IsAny<CancellationToken>()))
            .Callback<ConviteNegocio, CancellationToken>((convite, _) =>
            {
                convite.Id = 30;
                conviteCriado = convite;
            })
            .Returns(Task.CompletedTask);
        _tokenService
            .Setup(s => s.HashToken(It.IsAny<string>()))
            .Callback<string>(token => tokenHasheado = token)
            .Returns("hash-token");
        _tokenService
            .Setup(s => s.ProtegerToken(It.IsAny<string>()))
            .Returns<string>(token => $"protected:{token}");

        var service = CreateService();
        var antes = DateTime.UtcNow;

        var response = await service.CriarLinkAsync(20, new CriarConviteLinkRequestDto
        {
            Role = EstablishmentUserRole.Receptionist,
            LimiteUsuarios = 2
        });

        Assert.NotNull(conviteCriado);
        Assert.Equal(string.Empty, conviteCriado!.Email);
        Assert.Equal(StatusConviteNegocio.Ativo, conviteCriado.Status);
        Assert.Equal(2, conviteCriado.LimiteUsuarios);
        Assert.Equal(0, conviteCriado.QuantidadeUtilizacoes);
        Assert.Equal(EstablishmentUserRole.Receptionist, conviteCriado.RoleSugerida);
        Assert.True(conviteCriado.ExpiraEm >= antes.AddDays(1).AddMinutes(-1));
        Assert.True(conviteCriado.ExpiraEm <= DateTime.UtcNow.AddDays(1).AddMinutes(1));
        Assert.StartsWith("https://landing.test/convite/", response.LinkConvite);
        var uuidNoLink = response.LinkConvite["https://landing.test/convite/".Length..];
        Assert.True(Guid.TryParse(uuidNoLink, out _));
        Assert.Equal(uuidNoLink, tokenHasheado);
        Assert.Equal($"protected:{uuidNoLink}", conviteCriado!.TokenProtegido);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.ConviteLinkCriado,
            nameof(ConviteNegocio),
            30,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarLinkAsync_DeveRespeitarDuracaoEmHoras()
    {
        ConviteNegocio? conviteCriado = null;
        _conviteRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ConviteNegocio>(), It.IsAny<CancellationToken>()))
            .Callback<ConviteNegocio, CancellationToken>((convite, _) => conviteCriado = convite)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var antes = DateTime.UtcNow;

        await service.CriarLinkAsync(20, new CriarConviteLinkRequestDto
        {
            Role = EstablishmentUserRole.Profissional,
            LimiteUsuarios = 5,
            DuracaoValor = 48,
            DuracaoUnidade = UnidadeDuracaoConvite.Horas
        });

        Assert.NotNull(conviteCriado);
        Assert.True(conviteCriado!.ExpiraEm >= antes.AddHours(48).AddMinutes(-1));
        Assert.True(conviteCriado.ExpiraEm <= DateTime.UtcNow.AddHours(48).AddMinutes(1));
        Assert.Equal(TipoConviteNegocio.Profissional, conviteCriado.TipoConvite);
    }

    [Fact]
    public async Task CriarLinkAsync_DeveRejeitarLimiteInvalido()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioInvalidoException>(() =>
            service.CriarLinkAsync(20, new CriarConviteLinkRequestDto
            {
                Role = EstablishmentUserRole.Admin,
                LimiteUsuarios = 3
            }));
    }

    [Fact]
    public async Task ObterPreviewAsync_DeveFalharComMensagemUnificadaQuandoExpirado()
    {
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConviteNegocio
            {
                Id = 1,
                EstabelecimentoId = 20,
                Status = StatusConviteNegocio.Ativo,
                LimiteUsuarios = 2,
                QuantidadeUtilizacoes = 0,
                ExpiraEm = DateTime.UtcNow.AddMinutes(-1),
                TokenHash = "hash-token",
                Estabelecimento = new Estabelecimento { Id = 20, Nome = "Barbearia" }
            });

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ConviteNegocioIndisponivelException>(() =>
            service.ObterPreviewAsync(TokenUuid));

        Assert.Equal(ConviteNegocioIndisponivelException.MensagemPadrao, ex.Message);
    }

    [Fact]
    public async Task ObterPreviewAsync_DeveFalharQuandoEsgotado()
    {
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConviteNegocio
            {
                Id = 1,
                EstabelecimentoId = 20,
                Status = StatusConviteNegocio.Ativo,
                LimiteUsuarios = 1,
                QuantidadeUtilizacoes = 1,
                ExpiraEm = DateTime.UtcNow.AddDays(1),
                TokenHash = "hash-token"
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioIndisponivelException>(() =>
            service.ObterPreviewAsync(TokenUuid));
    }

    [Fact]
    public async Task ObterPreviewAsync_DeveRejeitarTokenQueNaoEUuid()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioNaoEncontradoException>(() =>
            service.ObterPreviewAsync("token-invalido"));
    }

    [Fact]
    public async Task AceitarAsync_DeveVincularUsuarioERegistrarUtilizacao()
    {
        var usuario = CriarUsuario();
        var convite = CriarConviteAtivo();
        EstabelecimentoUsuario? vinculo = null;

        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((v, _) => vinculo = v)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var response = await service.AceitarAsync(TokenUuid);

        Assert.NotNull(vinculo);
        Assert.Equal(EstablishmentUserRole.Receptionist, vinculo!.RoleNoEstabelecimento);
        Assert.Equal(1, response.QuantidadeUtilizacoes);
        _conviteRepository.Verify(r => r.AdicionarUtilizacaoAsync(
            It.Is<ConviteNegocioUtilizacao>(u => u.UsuarioId == 10 && u.ConviteNegocioId == 40),
            It.IsAny<CancellationToken>()), Times.Once);
        _conviteRepository.Verify(r => r.TentarRegistrarUtilizacaoAsync(
            40, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AceitarAsync_DeveBloquearReusoDoMesmoUsuario()
    {
        var usuario = CriarUsuario();
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConviteAtivo());
        _conviteRepository.Setup(r => r.UsuarioJaUtilizouAsync(40, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioInvalidoException>(() => service.AceitarAsync(TokenUuid));
        _conviteRepository.Verify(r => r.TentarRegistrarUtilizacaoAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AceitarAsync_DeveMarcarEsgotadoAoAtingirLimite()
    {
        var usuario = CriarUsuario();
        var convite = CriarConviteAtivo();
        convite.LimiteUsuarios = 1;
        convite.QuantidadeUtilizacoes = 0;

        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var response = await service.AceitarAsync(TokenUuid);

        Assert.Equal(StatusConviteNegocio.Esgotado.ToString(), response.Status);
        Assert.Equal(1, response.QuantidadeUtilizacoes);
    }

    [Fact]
    public async Task AceitarComCadastroAsync_DeveCriarUsuarioEAceitar()
    {
        var novoUsuario = CriarUsuario();
        novoUsuario.Id = 99;
        var convite = CriarConviteAtivo();

        _usuarioService
            .Setup(s => s.CadastrarClienteAsync(It.IsAny<CadastrarClienteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(novoUsuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var response = await service.AceitarComCadastroAsync(TokenUuid, new CadastrarClienteDto
        {
            Nome = "Novo",
            Email = "novo@email.com",
            Telefone = "11999999999",
            Senha = "Senha@123"
        });

        Assert.Equal(1, response.QuantidadeUtilizacoes);
        _usuarioService.Verify(s => s.CadastrarClienteAsync(
            It.IsAny<CadastrarClienteDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterLinkAsync_DeveRetornarLinkParaConviteAtivo()
    {
        var token = Guid.NewGuid().ToString("D");
        var convite = CriarConviteAtivo();
        convite.TokenProtegido = "protected-token";

        _conviteRepository.Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);
        _tokenService.Setup(s => s.DesprotegerToken("protected-token")).Returns(token);

        var service = CreateService();
        var response = await service.ObterLinkAsync(20, 40);

        Assert.StartsWith("https://landing.test/convite/", response.LinkConvite);
        Assert.EndsWith(token, response.LinkConvite);
        Assert.Equal(StatusConviteNegocio.Ativo.ToString(), response.Status);
    }

    [Fact]
    public async Task ObterLinkAsync_DeveFalharQuandoConviteNaoEstiverAtivo()
    {
        var convite = CriarConviteAtivo();
        convite.Status = StatusConviteNegocio.Cancelado;
        convite.TokenProtegido = "protected-token";

        _conviteRepository.Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);

        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioIndisponivelException>(() =>
            service.ObterLinkAsync(20, 40));
    }

    [Fact]
    public async Task ObterLinkAsync_DeveFalharQuandoTokenNaoForRecuperavel()
    {
        var convite = CriarConviteAtivo();
        convite.TokenProtegido = null;

        _conviteRepository.Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ConviteNegocioInvalidoException>(() =>
            service.ObterLinkAsync(20, 40));

        Assert.Contains("link recuperável", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelarAsync_DeveCancelarConviteAtivo()
    {
        var convite = CriarConviteAtivo();
        _conviteRepository.Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);

        var service = CreateService();
        var response = await service.CancelarAsync(20, 40);

        Assert.Equal(StatusConviteNegocio.Cancelado.ToString(), response.Status);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.ConviteLinkCancelado,
            nameof(ConviteNegocio),
            40,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private ConviteNegocioService CreateService() =>
        new(
            _conviteRepository.Object,
            _usuarioRepository.Object,
            _profissionalRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _autorizacaoNegocioService.Object,
            _modulosAssinaturaService.Object,
            _auditoriaNegocioService.Object,
            _usuarioService.Object,
            _tokenService.Object,
            _currentUserContext.Object,
            Options.Create(new AuthOptions
            {
                FrontendBaseUrl = "https://app.test",
                LandingBaseUrl = "https://landing.test"
            }));

    private static Usuario CriarUsuario() =>
        new()
        {
            Id = 10,
            Nome = "Joao",
            Email = "joao@email.com",
            Telefone = "11988887777",
            Ativo = true
        };

    private static ConviteNegocio CriarConviteAtivo() =>
        new()
        {
            Id = 40,
            EstabelecimentoId = 20,
            TipoConvite = TipoConviteNegocio.UsuarioEquipe,
            RoleSugerida = EstablishmentUserRole.Receptionist,
            Status = StatusConviteNegocio.Ativo,
            TokenHash = "hash-token",
            LimiteUsuarios = 2,
            QuantidadeUtilizacoes = 0,
            ExpiraEm = DateTime.UtcNow.AddDays(1),
            Estabelecimento = new Estabelecimento { Id = 20, Nome = "Barbearia Glow" }
        };
}
