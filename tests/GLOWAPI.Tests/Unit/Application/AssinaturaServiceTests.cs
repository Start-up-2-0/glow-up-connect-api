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
    public async Task IniciarAsync_DeveCriarEstabelecimentoEVinculoOwner_QuandoInformarDadosDoEstabelecimento()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        Estabelecimento? estabelecimentoCriado = null;
        _estabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Estabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<Estabelecimento, CancellationToken>((estabelecimento, _) =>
            {
                estabelecimento.Id = 50;
                estabelecimentoCriado = estabelecimento;
            })
            .Returns(Task.CompletedTask);

        EstabelecimentoUsuario? vinculoCriado = null;
        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((vinculo, _) => vinculoCriado = vinculo)
            .Returns(Task.CompletedTask);

        _assinaturaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()))
            .Callback<Assinatura, CancellationToken>((assinatura, _) => assinatura.Id = 60)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.IniciarAsync(new IniciarAssinaturaRequestDto
        {
            PlanoId = 1,
            TipoAssinatura = TipoAssinatura.Estabelecimento,
            Estabelecimento = new CriarEstabelecimentoAssinaturaDto
            {
                Nome = " Studio Glow ",
                Descricao = " Salao premium ",
                Telefone = "11999999999",
                Email = "studio@email.com"
            }
        });

        Assert.Equal(60, response.Id);
        Assert.Equal(50, response.EstabelecimentoId);
        Assert.Equal("PendentePagamento", response.Status);

        Assert.NotNull(estabelecimentoCriado);
        Assert.Equal("Studio Glow", estabelecimentoCriado!.Nome);
        Assert.Equal("Salao premium", estabelecimentoCriado.Descricao);
        Assert.True(estabelecimentoCriado.Ativo);
        Assert.NotEqual(Guid.Empty, estabelecimentoCriado.PublicGuid);

        Assert.NotNull(vinculoCriado);
        Assert.Equal(10, vinculoCriado!.UsuarioId);
        Assert.Equal(EstablishmentUserRole.Owner, vinculoCriado.RoleNoEstabelecimento);
        Assert.True(vinculoCriado.Ativo);
        Assert.Same(estabelecimentoCriado, vinculoCriado.Estabelecimento);

        _estabelecimentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Estabelecimento>(), It.IsAny<CancellationToken>()), Times.Once);
        _estabelecimentoUsuarioRepository.Verify(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoNomeDoNovoEstabelecimentoNaoForInformado()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<EstabelecimentoAssinaturaInvalidoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                Estabelecimento = new CriarEstabelecimentoAssinaturaDto
                {
                    Nome = " "
                }
            }));
    }

    [Fact]
    public async Task IniciarAsync_DeveCriarProfissionalAutonomoEAssinaturaPendente_QuandoInformarDadosDoAutonomo()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profissional?)null);

        Profissional? profissionalCriado = null;
        _profissionalRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()))
            .Callback<Profissional, CancellationToken>((profissional, _) =>
            {
                profissional.Id = 70;
                profissionalCriado = profissional;
            })
            .Returns(Task.CompletedTask);

        _assinaturaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()))
            .Callback<Assinatura, CancellationToken>((assinatura, _) => assinatura.Id = 80)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.IniciarAsync(new IniciarAssinaturaRequestDto
        {
            PlanoId = 1,
            TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            ProfissionalAutonomo = new CriarProfissionalAutonomoAssinaturaDto
            {
                NomePublico = " Maria Glow ",
                Biografia = " Especialista em beleza "
            }
        });

        Assert.Equal(80, response.Id);
        Assert.Equal(70, response.ProfissionalAutonomoId);
        Assert.Null(response.EstabelecimentoId);
        Assert.Equal("PendentePagamento", response.Status);

        Assert.NotNull(profissionalCriado);
        Assert.Equal(10, profissionalCriado!.UsuarioId);
        Assert.Equal("Maria Glow", profissionalCriado.NomePublico);
        Assert.Equal("Especialista em beleza", profissionalCriado.Biografia);
        Assert.Equal(ProfessionalType.Autonomo, profissionalCriado.TipoProfissional);
        Assert.True(profissionalCriado.Ativo);
        Assert.NotEqual(Guid.Empty, profissionalCriado.PublicGuid);

        _profissionalRepository.Verify(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_DeveAtivarProfissionalAutonomoExistente_QuandoPerfilEstiverInativo()
    {
        var profissional = new Profissional
        {
            Id = 70,
            UsuarioId = 10,
            NomePublico = "Nome antigo",
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = false
        };

        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);

        _assinaturaRepository
            .Setup(r => r.ExisteAtivaOuPendentePorProfissionalAutonomoAsync(70, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _assinaturaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()))
            .Callback<Assinatura, CancellationToken>((assinatura, _) => assinatura.Id = 80)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.IniciarAsync(new IniciarAssinaturaRequestDto
        {
            PlanoId = 1,
            TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            ProfissionalAutonomo = new CriarProfissionalAutonomoAssinaturaDto
            {
                NomePublico = "Novo nome",
                Biografia = "Nova bio"
            }
        });

        Assert.Equal(70, response.ProfissionalAutonomoId);
        Assert.Equal("Novo nome", profissional.NomePublico);
        Assert.Equal("Nova bio", profissional.Biografia);
        Assert.True(profissional.Ativo);
        Assert.NotNull(profissional.UpdatedAt);

        _profissionalRepository.Verify(r => r.Atualizar(profissional), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoNomePublicoDoAutonomoNaoForInformado()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 1, Ativo = true });

        _profissionalRepository
            .Setup(r => r.ObterPorUsuarioIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profissional?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalAutonomoAssinaturaInvalidoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
                ProfissionalAutonomo = new CriarProfissionalAutonomoAssinaturaDto
                {
                    NomePublico = " "
                }
            }));
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
