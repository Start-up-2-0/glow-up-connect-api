using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EquipeNegocioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
    private readonly Mock<IEquipeNotificacaoService> _equipeNotificacaoService = new();

    public EquipeNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.EquipeGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.EquipeGerenciar }));
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ProfissionalConvidar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ProfissionalConvidar }));
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ProfissionalGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ProfissionalGerenciar }));

        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarModulosPlus());
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                20,
                ModuloAssinatura.Profissionais,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _profissionalRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveCriarVinculo_QuandoUsuarioExistePorEmail()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        EstabelecimentoUsuario? vinculoCriado = null;

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((vinculo, _) => vinculoCriado = vinculo)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
        {
            Email = " MARIA@EMAIL.COM ",
            Role = EstablishmentUserRole.Receptionist
        });

        Assert.NotNull(vinculoCriado);
        Assert.Equal(20, vinculoCriado!.EstabelecimentoId);
        Assert.Equal(30, vinculoCriado.UsuarioId);
        Assert.Equal(EstablishmentUserRole.Receptionist, vinculoCriado.RoleNoEstabelecimento);
        Assert.True(vinculoCriado.Ativo);
        Assert.Equal("Maria", response.Nome);
        Assert.Equal(EstablishmentUserRole.Receptionist, response.Role);
        _estabelecimentoUsuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado,
            nameof(EstabelecimentoUsuario),
            It.IsAny<int?>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _equipeNotificacaoService.Verify(s => s.UsuarioEquipeConvidadoAsync(
            It.Is<EstabelecimentoUsuario>(v => v.UsuarioId == 30),
            usuario,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveBuscarPorTelefone_QuandoEmailNaoForInformado()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Joao",
            Email = "joao@email.com",
            Telefone = "11888888888",
            Ativo = true
        };

        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneAsync("11888888888", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var response = await service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
        {
            Telefone = " 11888888888 ",
            Role = EstablishmentUserRole.Manager
        });

        Assert.Equal(30, response.UsuarioId);
        Assert.Equal(EstablishmentUserRole.Manager, response.Role);
        _usuarioRepository.Verify(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveLancarExcecao_QuandoVinculoAtivoJaExiste()
    {
        var usuario = new Usuario { Id = 30, Email = "maria@email.com", Ativo = true };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 30, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioEquipeNegocioDuplicadoException>(() =>
            service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
            {
                Email = "maria@email.com",
                Role = EstablishmentUserRole.Receptionist
            }));
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveReativarVinculoInativo()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var vinculo = new EstabelecimentoUsuario
        {
            Id = 40,
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
            Ativo = false
        };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var response = await service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
        {
            Email = "maria@email.com",
            Role = EstablishmentUserRole.Admin
        });

        Assert.True(vinculo.Ativo);
        Assert.Equal(EstablishmentUserRole.Admin, vinculo.RoleNoEstabelecimento);
        Assert.NotNull(vinculo.UpdatedAt);
        Assert.Equal(EstablishmentUserRole.Admin, response.Role);
        _estabelecimentoUsuarioRepository.Verify(r => r.Atualizar(vinculo), Times.Once);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.EquipeGerenciar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
            {
                Email = "maria@email.com",
                Role = EstablishmentUserRole.Admin
            }));

        _usuarioRepository.Verify(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveLancarExcecao_QuandoLimiteUsuariosFoiAtingido()
    {
        var usuario = new Usuario { Id = 30, Email = "maria@email.com", Ativo = true };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarModulosBasic());
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteUsuariosNegocioExcedidoException>(() =>
            service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
            {
                Email = "maria@email.com",
                Role = EstablishmentUserRole.Receptionist
            }));

        _estabelecimentoUsuarioRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CadastrarUsuarioAsync_DeveLancarExcecao_QuandoUsuarioNaoExiste()
    {
        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("naoexiste@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioEquipeNegocioNaoEncontradoException>(() =>
            service.CadastrarUsuarioAsync(20, new CadastrarUsuarioEquipeRequestDto
            {
                Email = "naoexiste@email.com",
                Role = EstablishmentUserRole.Receptionist
            }));
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveCriarPerfilProfissionalEVinculos_QuandoUsuarioNaoTemPerfil()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        Profissional? profissionalCriado = null;
        ProfissionalEstabelecimento? vinculoProfissionalCriado = null;
        EstabelecimentoUsuario? vinculoUsuarioCriado = null;

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profissional?)null);
        _profissionalRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()))
            .Callback<Profissional, CancellationToken>((profissional, _) =>
            {
                profissional.Id = 70;
                profissionalCriado = profissional;
            })
            .Returns(Task.CompletedTask);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((vinculo, _) => vinculoUsuarioCriado = vinculo)
            .Returns(Task.CompletedTask);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalEstabelecimento, CancellationToken>((vinculo, _) => vinculoProfissionalCriado = vinculo)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
        {
            Email = " MARIA@EMAIL.COM ",
            NomePublico = " Maria Beauty ",
            Biografia = " Especialista ",
            Logo = " logo.png ",
            PodeReceberAgendamento = true
        });

        Assert.NotNull(profissionalCriado);
        Assert.Equal(30, profissionalCriado!.UsuarioId);
        Assert.Equal("Maria Beauty", profissionalCriado.NomePublico);
        Assert.Equal("Especialista", profissionalCriado.Biografia);
        Assert.Equal("logo.png", profissionalCriado.Logo);
        Assert.Equal(ProfessionalType.VinculadoEstabelecimento, profissionalCriado.TipoProfissional);
        Assert.NotNull(vinculoUsuarioCriado);
        Assert.Equal(EstablishmentUserRole.Profissional, vinculoUsuarioCriado!.RoleNoEstabelecimento);
        Assert.NotNull(vinculoProfissionalCriado);
        Assert.Equal(20, vinculoProfissionalCriado!.EstabelecimentoId);
        Assert.Equal(70, vinculoProfissionalCriado.ProfissionalId);
        Assert.True(vinculoProfissionalCriado.PodeReceberAgendamento);
        Assert.Equal(70, response.ProfissionalId);
        Assert.Equal("Maria Beauty", response.NomePublico);
        _equipeNotificacaoService.Verify(s => s.ProfissionalConvidadoAsync(
            It.Is<ProfissionalEstabelecimento>(v => v.ProfissionalId == 70),
            It.Is<Profissional>(p => p.Id == 70),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveVincularPerfilExistenteSemAlterarRoleAdministrativa()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var profissional = new Profissional
        {
            Id = 70,
            UsuarioId = 30,
            NomePublico = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = true
        };
        var vinculoUsuario = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Admin,
            Ativo = true
        };
        ProfissionalEstabelecimento? vinculoProfissionalCriado = null;

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoUsuario);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalEstabelecimento, CancellationToken>((vinculo, _) => vinculoProfissionalCriado = vinculo)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
        {
            Email = "maria@email.com",
            PodeReceberAgendamento = false
        });

        Assert.Equal(ProfessionalType.VinculadoEstabelecimento, profissional.TipoProfissional);
        Assert.Equal(EstablishmentUserRole.Admin, vinculoUsuario.RoleNoEstabelecimento);
        Assert.NotNull(vinculoProfissionalCriado);
        Assert.False(vinculoProfissionalCriado!.PodeReceberAgendamento);
        _estabelecimentoUsuarioRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveLancarExcecao_QuandoVinculoProfissionalAtivoJaExiste()
    {
        var usuario = new Usuario { Id = 30, Email = "maria@email.com", Ativo = true };
        var profissional = new Profissional { Id = 70, UsuarioId = 30, Ativo = true };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 70,
                EstabelecimentoId = 20,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalNegocioDuplicadoException>(() =>
            service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
            {
                Email = "maria@email.com"
            }));
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveReativarVinculoProfissionalInativo()
    {
        var usuario = new Usuario { Id = 30, Email = "maria@email.com", Ativo = true };
        var profissional = new Profissional { Id = 70, UsuarioId = 30, Email = "maria@email.com", Ativo = true };
        var vinculoProfissional = new ProfissionalEstabelecimento
        {
            Id = 90,
            ProfissionalId = 70,
            EstabelecimentoId = 20,
            Ativo = false,
            PodeReceberAgendamento = false,
            DataSaida = DateTime.UtcNow.AddDays(-1)
        };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculoProfissional);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var response = await service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
        {
            Email = "maria@email.com",
            PodeReceberAgendamento = true
        });

        Assert.True(vinculoProfissional.Ativo);
        Assert.True(vinculoProfissional.PodeReceberAgendamento);
        Assert.Null(vinculoProfissional.DataSaida);
        Assert.NotNull(vinculoProfissional.UpdatedAt);
        Assert.True(response.Ativo);
        _profissionalEstabelecimentoRepository.Verify(r => r.Atualizar(vinculoProfissional), Times.Once);
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveLancarExcecao_QuandoLimiteProfissionaisFoiAtingido()
    {
        var usuario = new Usuario { Id = 30, Email = "maria@email.com", Ativo = true };
        var profissional = new Profissional { Id = 70, UsuarioId = 30, Ativo = true };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarModulosBasic());
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteProfissionaisNegocioExcedidoException>(() =>
            service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
            {
                Email = "maria@email.com"
            }));
    }

    [Fact]
    public async Task ConvidarProfissionalAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ProfissionalConvidar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.ConvidarProfissionalAsync(20, new ConvidarProfissionalEquipeRequestDto
            {
                Email = "maria@email.com"
            }));

        _usuarioRepository.Verify(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarRoleUsuarioAsync_DeveAlterarRoleDoUsuario()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
            Ativo = true
        };

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        var response = await service.AtualizarRoleUsuarioAsync(
            20,
            30,
            new AtualizarRoleUsuarioEquipeRequestDto { Role = EstablishmentUserRole.Manager });

        Assert.Equal(EstablishmentUserRole.Manager, vinculo.RoleNoEstabelecimento);
        Assert.NotNull(vinculo.UpdatedAt);
        Assert.Equal(EstablishmentUserRole.Manager, response.Role);
        _estabelecimentoUsuarioRepository.Verify(r => r.Atualizar(vinculo), Times.Once);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeRoleAlterada,
            nameof(EstabelecimentoUsuario),
            vinculo.Id,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _equipeNotificacaoService.Verify(s => s.RoleUsuarioAlteradaAsync(
            vinculo,
            usuario,
            EstablishmentUserRole.Receptionist,
            EstablishmentUserRole.Manager,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarRoleUsuarioAsync_DeveImpedirRebaixarUltimoOwner()
    {
        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        };

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarOwnersAtivosAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = CreateService();

        await Assert.ThrowsAsync<UltimoOwnerNegocioException>(() =>
            service.AtualizarRoleUsuarioAsync(
                20,
                30,
                new AtualizarRoleUsuarioEquipeRequestDto { Role = EstablishmentUserRole.Admin }));
    }

    [Fact]
    public async Task AtualizarStatusUsuarioAsync_DeveInativarUsuarioDaEquipe()
    {
        var usuario = new Usuario
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
            Ativo = true
        };

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        var response = await service.AtualizarStatusUsuarioAsync(
            20,
            30,
            new AtualizarStatusUsuarioEquipeRequestDto { Ativo = false });

        Assert.False(vinculo.Ativo);
        Assert.False(response.Ativo);
        Assert.NotNull(vinculo.UpdatedAt);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeStatusAlterado,
            nameof(EstabelecimentoUsuario),
            vinculo.Id,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _equipeNotificacaoService.Verify(s => s.StatusUsuarioAlteradoAsync(
            vinculo,
            usuario,
            true,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusUsuarioAsync_DeveImpedirInativarUltimoOwner()
    {
        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        };

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarOwnersAtivosAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = CreateService();

        await Assert.ThrowsAsync<UltimoOwnerNegocioException>(() =>
            service.AtualizarStatusUsuarioAsync(
                20,
                30,
                new AtualizarStatusUsuarioEquipeRequestDto { Ativo = false }));
    }

    [Fact]
    public async Task AtualizarStatusUsuarioAsync_DeveValidarLimiteAoReativarUsuario()
    {
        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = 20,
            UsuarioId = 30,
            RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
            Ativo = false
        };

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterPorUsuarioAsync(20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarModulosBasic());
        _estabelecimentoUsuarioRepository
            .Setup(r => r.ContarAtivosAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteUsuariosNegocioExcedidoException>(() =>
            service.AtualizarStatusUsuarioAsync(
                20,
                30,
                new AtualizarStatusUsuarioEquipeRequestDto { Ativo = true }));
    }

    [Fact]
    public async Task AtualizarStatusProfissionalAsync_DeveInativarProfissionalEImpedirNovosAgendamentos()
    {
        var profissional = new Profissional
        {
            Id = 70,
            UsuarioId = 30,
            NomePublico = "Maria Beauty",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var vinculo = new ProfissionalEstabelecimento
        {
            Id = 90,
            EstabelecimentoId = 20,
            ProfissionalId = 70,
            PodeReceberAgendamento = true,
            Ativo = true
        };

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(70, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);

        var service = CreateService();

        var response = await service.AtualizarStatusProfissionalAsync(
            20,
            70,
            new AtualizarStatusProfissionalEquipeRequestDto { Ativo = false });

        Assert.False(vinculo.Ativo);
        Assert.False(vinculo.PodeReceberAgendamento);
        Assert.NotNull(vinculo.DataSaida);
        Assert.False(response.Ativo);
        Assert.False(response.PodeReceberAgendamento);
        _auditoriaNegocioService.Verify(s => s.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.ProfissionalStatusAlterado,
            nameof(ProfissionalEstabelecimento),
            vinculo.Id,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _equipeNotificacaoService.Verify(s => s.StatusProfissionalAlteradoAsync(
            vinculo,
            profissional,
            true,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusProfissionalAsync_DeveReativarProfissionalValidandoLimite()
    {
        var profissional = new Profissional
        {
            Id = 70,
            UsuarioId = 30,
            NomePublico = "Maria Beauty",
            Email = "maria@email.com",
            Telefone = "11999999999",
            Ativo = true
        };
        var vinculo = new ProfissionalEstabelecimento
        {
            Id = 90,
            EstabelecimentoId = 20,
            ProfissionalId = 70,
            PodeReceberAgendamento = false,
            Ativo = false,
            DataSaida = DateTime.UtcNow.AddDays(-1)
        };

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(70, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var response = await service.AtualizarStatusProfissionalAsync(
            20,
            70,
            new AtualizarStatusProfissionalEquipeRequestDto
            {
                Ativo = true,
                PodeReceberAgendamento = true
            });

        Assert.True(vinculo.Ativo);
        Assert.True(vinculo.PodeReceberAgendamento);
        Assert.Null(vinculo.DataSaida);
        Assert.True(response.Ativo);
    }

    [Fact]
    public async Task CadastrarProfissionalVitrineAsync_DeveCriarProfissionalSemUsuarioId_QuandoBasic()
    {
        Profissional? profissionalCriado = null;
        ProfissionalEstabelecimento? vinculoCriado = null;

        ConfigurarModulosBasicSemProfissionais();
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _profissionalRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()))
            .Callback<Profissional, CancellationToken>((profissional, _) =>
            {
                profissional.Id = 80;
                profissionalCriado = profissional;
            })
            .Returns(Task.CompletedTask);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalEstabelecimento, CancellationToken>((vinculo, _) => vinculoCriado = vinculo)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CadastrarProfissionalVitrineAsync(20, new CadastrarProfissionalVitrineRequestDto
        {
            NomePublico = " Ana Beauty ",
            Biografia = "Especialista em unhas"
        });

        Assert.NotNull(profissionalCriado);
        Assert.Null(profissionalCriado!.UsuarioId);
        Assert.Equal(ProfessionalType.SomenteExibicao, profissionalCriado.TipoProfissional);
        Assert.Equal("Ana Beauty", profissionalCriado.NomePublico);
        Assert.NotNull(vinculoCriado);
        Assert.True(vinculoCriado!.SomenteExibicao);
        Assert.False(vinculoCriado.PodeReceberAgendamento);
        Assert.Equal(80, response.ProfissionalId);
        Assert.True(response.SomenteExibicao);
        _estabelecimentoUsuarioRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CadastrarProfissionalVitrineAsync_DeveLancarExcecao_QuandoLimiteProfissionaisAtingido()
    {
        ConfigurarModulosBasicSemProfissionais();
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<LimiteProfissionaisNegocioExcedidoException>(() =>
            service.CadastrarProfissionalVitrineAsync(20, new CadastrarProfissionalVitrineRequestDto
            {
                NomePublico = "Ana Beauty"
            }));
    }

    [Fact]
    public async Task CadastrarProfissionalVitrineAsync_DeveLancarExcecao_QuandoPlanoPossuiModuloProfissionais()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalVitrineNegocioIndisponivelException>(() =>
            service.CadastrarProfissionalVitrineAsync(20, new CadastrarProfissionalVitrineRequestDto
            {
                NomePublico = "Ana Beauty"
            }));
    }

    [Fact]
    public async Task AtualizarStatusProfissionalAsync_DeveLancarExcecao_QuandoProfissionalNaoPertenceAoNegocio()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfissionalEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalNegocioNaoEncontradoException>(() =>
            service.AtualizarStatusProfissionalAsync(
                20,
                70,
                new AtualizarStatusProfissionalEquipeRequestDto { Ativo = false }));
    }

    private EquipeNegocioService CreateService() =>
        new(
            _usuarioRepository.Object,
            _profissionalRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _autorizacaoNegocioService.Object,
            _modulosAssinaturaService.Object,
            _auditoriaNegocioService.Object,
            _equipeNotificacaoService.Object);

    private static ModulosAssinaturaResponseDto CriarModulosPlus() =>
        ModulosAssinaturaResponseDto.Liberado(
            CriarAssinatura("Plus"),
            20,
            [ModuloAssinatura.Profissionais]);

    private static ModulosAssinaturaResponseDto CriarModulosBasic() =>
        ModulosAssinaturaResponseDto.Liberado(
            CriarAssinatura("Basic"),
            20,
            [ModuloAssinatura.Agenda, ModuloAssinatura.HorariosAtendimento]);

    private void ConfigurarModulosBasicSemProfissionais()
    {
        _modulosAssinaturaService
            .Setup(s => s.ObterPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarModulosBasic());
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                20,
                ModuloAssinatura.Profissionais,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private static Assinatura CriarAssinatura(string planoNome) =>
        new()
        {
            Id = 1,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Ativa,
            PlanoId = 2,
            Plano = new Plano
            {
                Id = 2,
                Nome = planoNome,
                LimiteProfissionais = planoNome == "Basic" ? 1 : null,
                Ativo = true
            }
        };
}
