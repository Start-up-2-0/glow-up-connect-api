using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
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
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IPagamentoRepository> _pagamentoRepository = new();
    private readonly Mock<IGatewayPagamentoResolver> _gatewayPagamentoResolver = new();
    private readonly Mock<IGatewayPagamento> _gatewayPagamento = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IAssinaturaNotificacaoService> _assinaturaNotificacaoService = new();
    private readonly Mock<IAssinaturaHistoricoService> _assinaturaHistoricoService = new();

    public AssinaturaServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
        _currentUser.Setup(c => c.Email).Returns("usuario@email.com");
        _gatewayPagamento.Setup(g => g.GatewaySuportado).Returns(GatewayPagamento.MercadoPago);
        _gatewayPagamento
            .Setup(g => g.CriarCobrancaAsync(
                It.IsAny<CriarCobrancaGatewayRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CriarCobrancaGatewayResponse(
                Sucesso: true,
                GatewayPaymentId: "pay_test_123",
                CheckoutUrl: "https://checkout.test/pay_test_123",
                QrCode: "qr-code",
                RequestPayload: "{}",
                ResponsePayload: "{}"));

        _gatewayPagamentoResolver
            .Setup(r => r.Resolver(GatewayPagamento.MercadoPago))
            .Returns(_gatewayPagamento.Object);

        _pagamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()))
            .Callback<Pagamento, CancellationToken>((pagamento, _) => pagamento.Id = 90)
            .Returns(Task.CompletedTask);
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
            EstabelecimentoId = 20,
            Pagamento = PagamentoValido()
        });

        Assert.Equal(30, response.Id);
        Assert.Equal(1, response.PlanoId);
        Assert.Equal(20, response.EstabelecimentoId);
        Assert.Null(response.ProfissionalAutonomoId);
        Assert.Equal("PendentePagamento", response.Status);
        Assert.Equal("MercadoPago", response.Gateway);
        Assert.NotNull(response.PagamentoInicial);
        Assert.Equal(90, response.PagamentoInicial!.Id);
        Assert.Equal("Pendente", response.PagamentoInicial.Status);
        Assert.Equal("pay_test_123", response.PagamentoInicial.GatewayPaymentId);
        Assert.Equal("https://checkout.test/pay_test_123", response.PagamentoInicial.CheckoutUrl);
        Assert.NotNull(assinaturaCriada);
        Assert.Equal(AssinaturaStatus.PendentePagamento, assinaturaCriada!.Status);

        _pagamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.AssinaturaIniciadaAsync(
            assinaturaCriada,
            It.Is<Plano>(plano => plano.Id == 1),
            "usuario@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
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
            Pagamento = PagamentoValido(),
            Estabelecimento = new CriarEstabelecimentoAssinaturaDto
            {
                Nome = " Studio Glow ",
                Descricao = " Salao premium ",
                Logo = "https://cdn.test/logo.png",
                Telefone = "11999999999",
                Email = "studio@email.com",
                Endereco = new()
                {
                    Cidade = "Sao Paulo",
                    Estado = "SP",
                    Local = "Rua Glow"
                }
            }
        });

        Assert.Equal(60, response.Id);
        Assert.Equal(50, response.EstabelecimentoId);
        Assert.Equal("PendentePagamento", response.Status);

        Assert.NotNull(estabelecimentoCriado);
        Assert.Equal("Studio Glow", estabelecimentoCriado!.Nome);
        Assert.Equal("Salao premium", estabelecimentoCriado.Descricao);
        Assert.Equal("https://cdn.test/logo.png", estabelecimentoCriado.Logo);
        Assert.Equal("11999999999", estabelecimentoCriado.Telefone);
        Assert.Equal("studio@email.com", estabelecimentoCriado.Email);
        Assert.NotNull(estabelecimentoCriado.Endereco);
        Assert.Equal("Sao Paulo", estabelecimentoCriado.Endereco!.Cidade);
        Assert.Equal("SP", estabelecimentoCriado.Endereco.Estado);
        Assert.Equal("Rua Glow", estabelecimentoCriado.Endereco.Logradouro);
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
        Estabelecimento? estabelecimentoCriado = null;
        ProfissionalEstabelecimento? vinculoProfissionalCriado = null;
        EstabelecimentoUsuario? vinculoOwnerCriado = null;

        _profissionalRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()))
            .Callback<Profissional, CancellationToken>((profissional, _) =>
            {
                profissional.Id = 70;
                profissionalCriado = profissional;
            })
            .Returns(Task.CompletedTask);

        _estabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Estabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<Estabelecimento, CancellationToken>((estabelecimento, _) =>
            {
                estabelecimento.Id = 71;
                estabelecimentoCriado = estabelecimento;
            })
            .Returns(Task.CompletedTask);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<EstabelecimentoUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<EstabelecimentoUsuario, CancellationToken>((vinculo, _) => vinculoOwnerCriado = vinculo)
            .Returns(Task.CompletedTask);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<ProfissionalEstabelecimento, CancellationToken>((vinculo, _) => vinculoProfissionalCriado = vinculo)
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
            Pagamento = PagamentoValido(),
            ProfissionalAutonomo = new CriarProfissionalAutonomoAssinaturaDto
            {
                NomePublico = " Maria Glow ",
                Biografia = " Especialista em beleza ",
                Logo = "https://cdn.test/maria.png",
                Telefone = "11988888888",
                Email = "maria@email.com",
                Endereco = new()
                {
                    Cidade = "Campinas",
                    Estado = "SP",
                    Local = "Sala 12"
                }
            }
        });

        Assert.Equal(80, response.Id);
        Assert.Null(response.ProfissionalAutonomoId);
        Assert.Equal(71, response.EstabelecimentoId);
        Assert.Equal("PendentePagamento", response.Status);

        Assert.NotNull(profissionalCriado);
        Assert.Equal(10, profissionalCriado!.UsuarioId);
        Assert.Equal("Maria Glow", profissionalCriado.NomePublico);
        Assert.Equal("Especialista em beleza", profissionalCriado.Biografia);
        Assert.Equal("https://cdn.test/maria.png", profissionalCriado.Logo);
        Assert.Equal("11988888888", profissionalCriado.Telefone);
        Assert.Equal("maria@email.com", profissionalCriado.Email);
        Assert.Equal(ProfessionalType.Autonomo, profissionalCriado.TipoProfissional);
        Assert.True(profissionalCriado.Ativo);
        Assert.NotEqual(Guid.Empty, profissionalCriado.PublicGuid);

        Assert.NotNull(estabelecimentoCriado);
        Assert.Equal("Maria Glow", estabelecimentoCriado!.Nome);
        Assert.Equal("Especialista em beleza", estabelecimentoCriado.Descricao);
        Assert.NotNull(estabelecimentoCriado.Endereco);
        Assert.Equal("Campinas", estabelecimentoCriado.Endereco!.Cidade);
        Assert.Equal("SP", estabelecimentoCriado.Endereco.Estado);
        Assert.Equal("Sala 12", estabelecimentoCriado.Endereco.Logradouro);
        Assert.NotNull(estabelecimentoCriado.Caixa);

        Assert.NotNull(vinculoOwnerCriado);
        Assert.Same(estabelecimentoCriado, vinculoOwnerCriado!.Estabelecimento);
        Assert.Equal(10, vinculoOwnerCriado.UsuarioId);
        Assert.Equal(EstablishmentUserRole.Owner, vinculoOwnerCriado.RoleNoEstabelecimento);

        Assert.NotNull(vinculoProfissionalCriado);
        Assert.Same(estabelecimentoCriado, vinculoProfissionalCriado!.Estabelecimento);
        Assert.Same(profissionalCriado, vinculoProfissionalCriado.Profissional);

        _profissionalRepository.Verify(r => r.AdicionarAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()), Times.Once);
        _estabelecimentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Estabelecimento>(), It.IsAny<CancellationToken>()), Times.Once);
        _profissionalEstabelecimentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<ProfissionalEstabelecimento>(), It.IsAny<CancellationToken>()), Times.Once);
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

        Estabelecimento? estabelecimentoCriado = null;
        _estabelecimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Estabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<Estabelecimento, CancellationToken>((estabelecimento, _) =>
            {
                estabelecimento.Id = 71;
                estabelecimentoCriado = estabelecimento;
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
            Pagamento = PagamentoValido(),
            ProfissionalAutonomo = new CriarProfissionalAutonomoAssinaturaDto
            {
                NomePublico = "Novo nome",
                Biografia = "Nova bio",
                Logo = "https://cdn.test/novo.png",
                Telefone = "11977777777",
                Email = "novo@email.com",
                Endereco = new()
                {
                    Cidade = "Santos",
                    Estado = "SP",
                    Local = "Av Praia"
                }
            }
        });

        Assert.Null(response.ProfissionalAutonomoId);
        Assert.Equal(71, response.EstabelecimentoId);
        Assert.Equal("Novo nome", profissional.NomePublico);
        Assert.Equal("Nova bio", profissional.Biografia);
        Assert.Equal("https://cdn.test/novo.png", profissional.Logo);
        Assert.Equal("11977777777", profissional.Telefone);
        Assert.Equal("novo@email.com", profissional.Email);
        Assert.True(profissional.Ativo);
        Assert.NotNull(profissional.UpdatedAt);
        Assert.NotNull(estabelecimentoCriado);
        Assert.Equal("Santos", estabelecimentoCriado!.Endereco!.Cidade);
        Assert.Equal("SP", estabelecimentoCriado.Endereco.Estado);
        Assert.Equal("Av Praia", estabelecimentoCriado.Endereco.Logradouro);

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
    public async Task IniciarAsync_DeveLancarExcecao_ENaoPersistir_QuandoGatewayFalhar()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano
            {
                Id = 1,
                Nome = "Plano Pro",
                Preco = 99.90m,
                Ativo = true
            });

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _assinaturaRepository
            .Setup(r => r.ExisteAtivaOuPendentePorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _gatewayPagamento
            .Setup(g => g.CriarCobrancaAsync(
                It.IsAny<CriarCobrancaGatewayRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarCobrancaGatewayResponse.Falha("{}", "{}", "Gateway indisponivel"));

        var service = CreateService();

        await Assert.ThrowsAsync<GatewayPagamentoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                EstabelecimentoId = 20,
                Pagamento = PagamentoValido()
            }));

        _assinaturaRepository.Verify(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()), Times.Never);
        _pagamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_ENaoChamarGateway_QuandoPagamentoMercadoPagoNaoForInformado()
    {
        _planoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano
            {
                Id = 1,
                Nome = "Plano Pro",
                Preco = 99.90m,
                Ativo = true
            });

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _assinaturaRepository
            .Setup(r => r.ExisteAtivaOuPendentePorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<PagamentoAssinaturaInvalidoException>(() =>
            service.IniciarAsync(new IniciarAssinaturaRequestDto
            {
                PlanoId = 1,
                TipoAssinatura = TipoAssinatura.Estabelecimento,
                EstabelecimentoId = 20
            }));

        _gatewayPagamento.Verify(g => g.CriarCobrancaAsync(
            It.IsAny<CriarCobrancaGatewayRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _assinaturaRepository.Verify(r => r.AdicionarAsync(It.IsAny<Assinatura>(), It.IsAny<CancellationToken>()), Times.Never);
        _pagamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact]
    public async Task TrocarPlanoAsync_DeveCriarPagamentoPendente_QuandoNovoPlanoExigirCobranca()
    {
        var assinatura = new Assinatura
        {
            Id = 30,
            PlanoId = 1,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Ativa,
            Gateway = GatewayPagamento.MercadoPago,
            Plano = new Plano
            {
                Id = 1,
                Nome = "Plano Basico",
                Preco = 49.90m,
                Periodo = PlanoPeriodo.Mensal
            }
        };

        var novoPlano = new Plano
        {
            Id = 2,
            Nome = "Plano Pro",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _planoRepository
            .Setup(r => r.ObterPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(novoPlano);

        Pagamento? pagamentoCriado = null;
        _pagamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()))
            .Callback<Pagamento, CancellationToken>((pagamento, _) =>
            {
                pagamento.Id = 91;
                pagamentoCriado = pagamento;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.TrocarPlanoAsync(30, new TrocarPlanoAssinaturaRequestDto
        {
            NovoPlanoId = 2,
            Pagamento = PagamentoValido()
        });

        Assert.Equal(30, response.Id);
        Assert.Equal(1, response.PlanoId);
        Assert.Equal(2, response.PlanoAlteracaoPendenteId);
        Assert.NotNull(response.PagamentoInicial);
        Assert.Equal(91, response.PagamentoInicial!.Id);
        Assert.Equal(99.90m, response.PagamentoInicial.Valor);
        Assert.Equal("pay_test_123", response.PagamentoInicial.GatewayPaymentId);

        Assert.NotNull(pagamentoCriado);
        Assert.Equal(30, pagamentoCriado!.AssinaturaId);
        Assert.Equal("Checkout", pagamentoCriado.MetodoPagamento);
        Assert.Equal(PagamentoStatus.Pendente, pagamentoCriado.Status);
        Assert.Equal(2, assinatura.PlanoAlteracaoPendenteId);
        Assert.Equal(1, assinatura.PlanoId);

        _assinaturaRepository.Verify(r => r.Atualizar(assinatura), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrocarPlanoAsync_DeveAtualizarPlanoImediatamente_QuandoNovoPlanoNaoExigirCobranca()
    {
        var assinatura = new Assinatura
        {
            Id = 30,
            PlanoId = 1,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Ativa,
            Gateway = GatewayPagamento.MercadoPago,
            Plano = new Plano
            {
                Id = 1,
                Nome = "Plano Solo A",
                Preco = 49.90m,
                Periodo = PlanoPeriodo.Mensal
            }
        };

        var novoPlano = new Plano
        {
            Id = 2,
            Nome = "Plano Solo B",
            Preco = 49.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _planoRepository
            .Setup(r => r.ObterPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(novoPlano);

        var service = CreateService();

        var response = await service.TrocarPlanoAsync(30, new TrocarPlanoAssinaturaRequestDto
        {
            NovoPlanoId = 2
        });

        Assert.Equal(2, response.PlanoId);
        Assert.Null(response.PlanoAlteracaoPendenteId);
        Assert.Null(response.PagamentoInicial);
        Assert.Equal(2, assinatura.PlanoId);
        Assert.Null(assinatura.PlanoAlteracaoPendenteId);
        Assert.NotNull(assinatura.UpdatedAt);

        _pagamentoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _assinaturaRepository.Verify(r => r.Atualizar(assinatura), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrocarPlanoAsync_DeveLancarExcecao_QuandoAssinaturaNaoEstaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 30,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.PendentePagamento,
                Plano = new Plano { Id = 1 }
            });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<TrocaPlanoAssinaturaInvalidaException>(() =>
            service.TrocarPlanoAsync(30, new TrocarPlanoAssinaturaRequestDto
            {
                NovoPlanoId = 2
            }));
    }

    [Fact]
    public async Task TrocarPlanoAsync_DeveLancarExcecao_QuandoNovoPlanoNaoExisteOuInativo()
    {
        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 30,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Ativa,
                PlanoId = 1,
                Plano = new Plano { Id = 1, Preco = 49.90m, Periodo = PlanoPeriodo.Mensal }
            });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        _planoRepository
            .Setup(r => r.ObterPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plano { Id = 2, Ativo = false });

        var service = CreateService();

        await Assert.ThrowsAsync<PlanoNaoEncontradoException>(() =>
            service.TrocarPlanoAsync(30, new TrocarPlanoAssinaturaRequestDto
            {
                NovoPlanoId = 2
            }));
    }

    [Fact]
    public async Task CancelarAsync_DeveCancelarAssinaturaAtiva_QuandoUsuarioTemPermissao()
    {
        var assinatura = new Assinatura
        {
            Id = 30,
            EstabelecimentoId = 20,
            Status = AssinaturaStatus.Ativa,
            RenovacaoAutomatica = true,
            PlanoAlteracaoPendenteId = 2,
            Plano = new Plano { Id = 1 }
        };

        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        var service = CreateService();

        var response = await service.CancelarAsync(30);

        Assert.Equal(30, response.Id);
        Assert.Equal("Cancelada", response.Status);
        Assert.Equal(AssinaturaStatus.Cancelada, assinatura.Status);
        Assert.NotNull(assinatura.CanceladoEm);
        Assert.False(assinatura.RenovacaoAutomatica);
        Assert.Null(assinatura.PlanoAlteracaoPendenteId);
        Assert.NotNull(assinatura.UpdatedAt);

        _assinaturaRepository.Verify(r => r.Atualizar(assinatura), Times.Once);
        _assinaturaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _assinaturaNotificacaoService.Verify(n => n.AssinaturaCanceladaAsync(
            assinatura,
            "usuario@email.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_DeveLancarExcecao_QuandoAssinaturaNaoEstaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 30,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Suspensa,
                Plano = new Plano { Id = 1 }
            });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<CancelamentoAssinaturaInvalidoException>(() =>
            service.CancelarAsync(30));
    }

    [Fact]
    public async Task CancelarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _assinaturaRepository
            .Setup(r => r.ObterPorIdComPlanoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 30,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano { Id = 1 }
            });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoUsuario?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.CancelarAsync(30));
    }

    private AssinaturaService CreateService() =>
        new(
            _assinaturaRepository.Object,
            _planoRepository.Object,
            _estabelecimentoRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _profissionalRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _pagamentoRepository.Object,
            _gatewayPagamentoResolver.Object,
            _currentUser.Object,
            _assinaturaNotificacaoService.Object,
            _assinaturaHistoricoService.Object);

    private static PagamentoTransparenteMercadoPagoDto PagamentoValido() =>
        new()
        {
            PaymentMethodId = "pix"
        };
}
