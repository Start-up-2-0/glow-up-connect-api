using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Mensageria;
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
    private readonly Mock<IConviteNegocioRepository> _conviteRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemNotificacaoService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
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
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ProfissionalConvidar, PermissaoNegocio.ProfissionalGerenciar }));
        _tokenService.Setup(s => s.GerarRefreshToken()).Returns("token-plano");
        _tokenService.Setup(s => s.HashToken("token-plano")).Returns("hash-token");
        _tokenService.Setup(s => s.HashToken("token")).Returns("hash-token");
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
                [ModuloAssinatura.Profissionais]));
    }

    [Fact]
    public async Task CriarConviteProfissionalAsync_DevePersistirConviteEEnfileirarEmail()
    {
        ConviteNegocio? conviteCriado = null;
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _conviteRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ConviteNegocio>(), It.IsAny<CancellationToken>()))
            .Callback<ConviteNegocio, CancellationToken>((convite, _) =>
            {
                convite.Id = 30;
                conviteCriado = convite;
            })
            .Returns(Task.CompletedTask);
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        var response = await service.CriarConviteProfissionalAsync(20, new CriarConviteProfissionalRequestDto
        {
            Email = " PROFISSIONAL@EMAIL.COM ",
            NomePublico = "Maria Beauty",
            PodeReceberAgendamento = true
        });

        Assert.NotNull(conviteCriado);
        Assert.Equal("profissional@email.com", conviteCriado!.Email);
        Assert.Equal(TipoConviteNegocio.Profissional, conviteCriado.TipoConvite);
        Assert.Equal(StatusConviteNegocio.Pendente, conviteCriado.Status);
        Assert.Equal("hash-token", conviteCriado.TokenHash);
        Assert.Equal(10, conviteCriado.CriadoPorUsuarioId);
        Assert.Equal(30, response.Id);
        Assert.NotNull(mensagem);
        Assert.Equal("profissional@email.com", mensagem!.Destinatario);
        Assert.Contains("token-plano", mensagem.Conteudo);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.ProfissionalConvidado,
            nameof(ConviteNegocio),
            30,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarConviteProfissionalAsync_DeveBloquearConvitePendenteDuplicado()
    {
        _conviteRepository
            .Setup(r => r.ObterPendentePorDestinatarioAsync(
                20,
                "profissional@email.com",
                TipoConviteNegocio.Profissional,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConviteNegocio
            {
                EstabelecimentoId = 20,
                Email = "profissional@email.com",
                Status = StatusConviteNegocio.Pendente,
                ExpiraEm = DateTime.UtcNow.AddDays(1)
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioDuplicadoException>(() =>
            service.CriarConviteProfissionalAsync(20, new CriarConviteProfissionalRequestDto
            {
                Email = "profissional@email.com"
            }));
    }

    [Fact]
    public async Task AceitarAsync_DeveCriarVinculosDoUsuarioEProfissional()
    {
        var usuario = CriarUsuario();
        EstabelecimentoUsuario? vinculoUsuario = null;
        ProfissionalEstabelecimento? vinculoProfissional = null;
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());
        _profissionalRepository.Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional { Id = 70, UsuarioId = 10, Email = usuario.Email, Telefone = usuario.Telefone, NomePublico = usuario.Nome });
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((vinculo, _) => vinculoUsuario = vinculo)
            .Returns(Task.CompletedTask);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalEstabelecimento, CancellationToken>((vinculo, _) => vinculoProfissional = vinculo)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.AceitarAsync("token");

        Assert.Equal("Aceito", response.Status);
        Assert.NotNull(vinculoUsuario);
        Assert.Equal(EstablishmentUserRole.Profissional, vinculoUsuario!.RoleNoEstabelecimento);
        Assert.NotNull(vinculoProfissional);
        Assert.Equal(70, vinculoProfissional!.ProfissionalId);
        Assert.True(vinculoProfissional.PodeReceberAgendamento);
    }

    [Fact]
    public async Task AceitarAsync_DeveResetarRoleAoReativarVinculoUsuarioInativo()
    {
        var usuario = CriarUsuario();
        var vinculoUsuario = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 10,
            RoleNoEstabelecimento = EstablishmentUserRole.Admin,
            Ativo = false
        };

        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoUsuario);
        _profissionalRepository.Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional { Id = 70, UsuarioId = 10, Email = usuario.Email, Telefone = usuario.Telefone, NomePublico = usuario.Nome });

        var service = CreateService();

        await service.AceitarAsync("token");

        Assert.True(vinculoUsuario.Ativo);
        Assert.Equal(EstablishmentUserRole.Profissional, vinculoUsuario.RoleNoEstabelecimento);
    }

    [Fact]
    public async Task AceitarAsync_DeveReativarPerfilProfissionalInativo()
    {
        var usuario = CriarUsuario();
        var profissional = new Profissional
        {
            Id = 70,
            UsuarioId = 10,
            Email = usuario.Email,
            Telefone = usuario.Telefone,
            NomePublico = usuario.Nome,
            Ativo = false,
            TipoProfissional = ProfessionalType.Autonomo
        };

        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());
        _profissionalRepository.Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);

        var service = CreateService();

        await service.AceitarAsync("token");

        Assert.True(profissional.Ativo);
        Assert.Equal(ProfessionalType.VinculadoEstabelecimento, profissional.TipoProfissional);
        Assert.NotNull(profissional.UpdatedAt);
        _profissionalRepository.Verify(r => r.Atualizar(profissional), Times.Once);
    }

    [Fact]
    public async Task AceitarAsync_DeveValidarLimiteAntesDeCriarVinculos()
    {
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModulosAssinaturaResponseDto.Liberado(
                new Assinatura
                {
                    Id = 1,
                    EstabelecimentoId = 20,
                    PlanoId = 2,
                    Status = AssinaturaStatus.Ativa,
                    Plano = new Plano { Id = 2, Nome = "Basic" }
                },
                [ModuloAssinatura.Profissionais]));
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(CriarUsuario());
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteUsuariosNegocioExcedidoException>(() =>
            service.AceitarAsync("token"));
    }

    [Fact]
    public async Task AceitarAsync_DeveValidarLimiteProfissionaisAntesDeCriarVinculoProfissional()
    {
        var usuario = CriarUsuario();
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModulosAssinaturaResponseDto.Liberado(
                new Assinatura
                {
                    Id = 1,
                    EstabelecimentoId = 20,
                    PlanoId = 2,
                    Status = AssinaturaStatus.Ativa,
                    Plano = new Plano { Id = 2, Nome = "Limitado", LimiteProfissionais = 1 }
                },
                [ModuloAssinatura.Profissionais]));
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Profissional,
                Ativo = true
            });
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());
        _profissionalRepository.Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional { Id = 70, UsuarioId = 10, Email = usuario.Email, Telefone = usuario.Telefone, NomePublico = usuario.Nome });

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteProfissionaisNegocioExcedidoException>(() =>
            service.AceitarAsync("token"));
    }

    [Fact]
    public async Task AceitarAsync_DeveBloquearDestinatarioDiferente()
    {
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 10, Nome = "Outra", Email = "outra@email.com", Ativo = true });
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarConvite());

        var service = CreateService();

        await Assert.ThrowsAsync<ConviteNegocioInvalidoException>(() =>
            service.AceitarAsync("token"));
    }

    [Fact]
    public async Task RejeitarAsync_DeveMarcarConviteComoRejeitado()
    {
        var convite = CriarConvite();
        _usuarioRepository.Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(CriarUsuario());
        _conviteRepository.Setup(r => r.ObterPorTokenHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(convite);

        var service = CreateService();

        var response = await service.RejeitarAsync("token");

        Assert.Equal(StatusConviteNegocio.Rejeitado, convite.Status);
        Assert.Equal("Rejeitado", response.Status);
        Assert.Equal(10, convite.AceitoPorUsuarioId);
        Assert.NotNull(convite.RespondidoEm);
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
            _mensagemNotificacaoService.Object,
            _auditoriaNegocioService.Object,
            _tokenService.Object,
            _currentUserContext.Object,
            Options.Create(new AuthOptions { FrontendBaseUrl = "https://app.test" }));

    private static Usuario CriarUsuario() =>
        new()
        {
            Id = 10,
            Nome = "Maria",
            Email = "profissional@email.com",
            Telefone = "11999999999",
            Ativo = true
        };

    private static ConviteNegocio CriarConvite() =>
        new()
        {
            Id = 30,
            EstabelecimentoId = 20,
            Email = "profissional@email.com",
            TipoConvite = TipoConviteNegocio.Profissional,
            RoleSugerida = EstablishmentUserRole.Profissional,
            Status = StatusConviteNegocio.Pendente,
            TokenHash = "hash-token",
            ExpiraEm = DateTime.UtcNow.AddDays(1),
            CriadoPorUsuarioId = 1,
            PodeReceberAgendamento = true
        };
}
