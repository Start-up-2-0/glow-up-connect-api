using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ModulosAssinaturaServiceTests
{
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();

    public ModulosAssinaturaServiceTests()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProfissionalEstabelecimento>());
    }
    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveLiberarModulosELimites_QuandoAssinaturaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                PlanoId = 2,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano
                {
                    Id = 2,
                    Nome = "Premium",
                    LimiteProfissionais = 5,
                    LimiteServicos = 30,
                    LimiteAgendamentos = 500
                }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(10);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal(1, resultado.AssinaturaId);
        Assert.Equal(2, resultado.PlanoId);
        Assert.Equal("Premium", resultado.PlanoNome);
        Assert.Equal("Ativa", resultado.Status);
        Assert.Equal("Estabelecimento", resultado.TipoAssinatura);
        Assert.Equal(10, resultado.EstabelecimentoId);
        Assert.Contains("Estabelecimento", resultado.Modulos);
        Assert.Contains("Profissionais", resultado.Modulos);
        Assert.Contains("WhatsApp", resultado.Modulos);
        Assert.Contains("Caixa", resultado.Modulos);
        Assert.Contains("Financeiro", resultado.Modulos);
        Assert.Contains("ComissaoProfissionais", resultado.Modulos);
        Assert.Equal(5, resultado.Limites.Profissionais);
        Assert.Equal(30, resultado.Limites.Servicos);
        Assert.Equal(500, resultado.Limites.Agendamentos);
        Assert.Null(resultado.Limites.Usuarios);
        Assert.Null(resultado.Limites.AgendamentosPorDia);
        Assert.True(resultado.Limites.PrioridadeListagemPublica);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_AutonomoEssencial_DeveLiberarClientesSemEquipe()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                PlanoId = 2,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Ativa,
                TipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
                Plano = new Plano
                {
                    Id = 2,
                    Nome = "Essencial",
                    LimiteEstabelecimentos = 1
                }
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProfissionalEstabelecimento
                {
                    Profissional = new Profissional
                    {
                        Id = 77,
                        Ativo = true,
                        TipoProfissional = ProfessionalType.Autonomo,
                        NomePublico = "Barbeiro Solo"
                    }
                }
            ]);

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(10);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal("ProfissionalAutonomo", resultado.TipoAssinatura);
        Assert.Equal(77, resultado.ProfissionalAutonomoId);
        Assert.Contains("Clientes", resultado.Modulos);
        Assert.DoesNotContain("Profissionais", resultado.Modulos);
        Assert.DoesNotContain("WhatsApp", resultado.Modulos);
        Assert.DoesNotContain("ComissaoProfissionais", resultado.Modulos);
        Assert.Equal(1, resultado.Limites.Usuarios);
        Assert.Equal(1, resultado.Limites.Estabelecimentos);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveBloquearModulos_QuandoAssinaturaNaoEstaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                PlanoId = 2,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.PendentePagamento,
                Plano = new Plano
                {
                    Id = 2,
                    Nome = "Plano Estudio",
                    LimiteProfissionais = 5,
                    LimiteServicos = 30,
                    LimiteAgendamentos = 500
                }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(10);

        Assert.False(resultado.AssinaturaAtiva);
        Assert.Equal("PendentePagamento", resultado.Status);
        Assert.Empty(resultado.Modulos);
        Assert.Equal(5, resultado.Limites.Profissionais);
        Assert.Equal(30, resultado.Limites.Servicos);
        Assert.Equal(500, resultado.Limites.Agendamentos);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveLiberarModulos_QuandoAssinaturaEmTrial()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                PlanoId = 2,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Trial,
                Plano = new Plano { Id = 2, Nome = "Plus" }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(10);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal("Trial", resultado.Status);
        Assert.Contains("Profissionais", resultado.Modulos);
    }

    [Theory]
    [InlineData(AssinaturaStatus.Cancelada)]
    [InlineData(AssinaturaStatus.Expirada)]
    [InlineData(AssinaturaStatus.Suspensa)]
    public async Task PossuiModuloPorEstabelecimentoAsync_DeveRetornarFalse_QuandoAssinaturaNaoEstaAtiva(
        AssinaturaStatus status)
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                EstabelecimentoId = 10,
                Status = status,
                Plano = new Plano { Id = 2, Nome = "Plano Estudio" }
            });

        var service = CreateService();

        var possuiModulo = await service.PossuiModuloPorEstabelecimentoAsync(10, ModuloAssinatura.Caixa);

        Assert.False(possuiModulo);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveLiberarPlanoBasicParaTenantAutonomo_QuandoAssinaturaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 3,
                PlanoId = 4,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano
                {
                    Id = 4,
                    Nome = "Basic",
                    LimiteProfissionais = 1,
                    LimiteServicos = 15,
                    LimiteAgendamentos = null
                }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(20);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal("Estabelecimento", resultado.TipoAssinatura);
        Assert.Equal(20, resultado.EstabelecimentoId);
        Assert.Null(resultado.ProfissionalAutonomoId);
        Assert.Contains("Estabelecimento", resultado.Modulos);
        Assert.Contains("Servicos", resultado.Modulos);
        Assert.Contains("HorariosAtendimento", resultado.Modulos);
        Assert.Contains("Notificacoes", resultado.Modulos);
        Assert.Contains("Email", resultado.Modulos);
        Assert.DoesNotContain("WhatsApp", resultado.Modulos);
        Assert.DoesNotContain("Caixa", resultado.Modulos);
        Assert.DoesNotContain("Financeiro", resultado.Modulos);
        Assert.DoesNotContain("ComissaoProfissionais", resultado.Modulos);
        Assert.DoesNotContain("Profissionais", resultado.Modulos);
        Assert.Equal(1, resultado.Limites.Profissionais);
        Assert.Equal(15, resultado.Limites.Servicos);
        Assert.Null(resultado.Limites.Agendamentos);
        Assert.Equal(1, resultado.Limites.Usuarios);
        Assert.Null(resultado.Limites.AgendamentosPorDia);
        Assert.False(resultado.Limites.PrioridadeListagemPublica);
    }

    [Fact]
    public async Task PossuiModuloPorEstabelecimentoAsync_DeveRetornarTrue_ParaTenantAutonomoQuandoModuloEstaLiberado()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 3,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano { Id = 4, Nome = "Basic" }
            });

        var service = CreateService();

        var possuiModulo = await service.PossuiModuloPorEstabelecimentoAsync(
            20,
            ModuloAssinatura.Agenda);

        Assert.True(possuiModulo);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveLiberarPlanoPlusSemFinanceiro()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano { Id = 2, Nome = "Plus" }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(10);

        Assert.Contains("Profissionais", resultado.Modulos);
        Assert.Contains("WhatsApp", resultado.Modulos);
        Assert.DoesNotContain("Caixa", resultado.Modulos);
        Assert.DoesNotContain("Financeiro", resultado.Modulos);
        Assert.DoesNotContain("ComissaoProfissionais", resultado.Modulos);
        Assert.Null(resultado.Limites.Usuarios);
        Assert.Null(resultado.Limites.AgendamentosPorDia);
        Assert.False(resultado.Limites.PrioridadeListagemPublica);
    }

    [Fact]
    public async Task PossuiModuloPorEstabelecimentoAsync_DeveRetornarFalse_QuandoPlanoBasicNaoLiberaCaixa()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano { Id = 2, Nome = "Basic" }
            });

        var service = CreateService();

        var possuiModulo = await service.PossuiModuloPorEstabelecimentoAsync(10, ModuloAssinatura.Caixa);

        Assert.False(possuiModulo);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveRetornarBloqueado_QuandoNaoExistirAssinatura()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Assinatura?)null);

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(20);

        Assert.False(resultado.AssinaturaAtiva);
        Assert.Null(resultado.AssinaturaId);
        Assert.Null(resultado.Status);
        Assert.Empty(resultado.Modulos);
        Assert.Null(resultado.Limites.Profissionais);
        Assert.Null(resultado.Limites.Servicos);
        Assert.Null(resultado.Limites.Agendamentos);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveHerdarModulosDaAssinaturaTitular_QuandoFilialVinculada()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 50,
                EstabelecimentoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano
                {
                    Id = 3,
                    Nome = "Premium",
                    LimiteEstabelecimentos = 5
                }
            });

        var service = CreateService();

        var resultado = await service.ObterPorEstabelecimentoAsync(99);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal(50, resultado.AssinaturaId);
        Assert.Equal(99, resultado.EstabelecimentoId);
        Assert.Contains("Financeiro", resultado.Modulos);
        Assert.Equal(5, resultado.Limites.Estabelecimentos);
    }

    private ModulosAssinaturaService CreateService() =>
        new(_assinaturaRepository.Object, _profissionalEstabelecimentoRepository.Object);
}
