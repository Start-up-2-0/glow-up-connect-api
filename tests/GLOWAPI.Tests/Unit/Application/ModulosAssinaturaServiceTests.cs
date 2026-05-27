using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ModulosAssinaturaServiceTests
{
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveLiberarModulosELimites_QuandoAssinaturaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                PlanoId = 2,
                EstabelecimentoId = 10,
                Status = AssinaturaStatus.Ativa,
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

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal(1, resultado.AssinaturaId);
        Assert.Equal(2, resultado.PlanoId);
        Assert.Equal("Plano Estudio", resultado.PlanoNome);
        Assert.Equal("Ativa", resultado.Status);
        Assert.Equal("Estabelecimento", resultado.TipoAssinatura);
        Assert.Equal(10, resultado.EstabelecimentoId);
        Assert.Contains("Estabelecimento", resultado.Modulos);
        Assert.Contains("Profissionais", resultado.Modulos);
        Assert.Contains("Caixa", resultado.Modulos);
        Assert.Equal(5, resultado.Limites.Profissionais);
        Assert.Equal(30, resultado.Limites.Servicos);
        Assert.Equal(500, resultado.Limites.Agendamentos);
    }

    [Fact]
    public async Task ObterPorEstabelecimentoAsync_DeveBloquearModulos_QuandoAssinaturaNaoEstaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
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

    [Theory]
    [InlineData(AssinaturaStatus.Cancelada)]
    [InlineData(AssinaturaStatus.Expirada)]
    [InlineData(AssinaturaStatus.Suspensa)]
    [InlineData(AssinaturaStatus.Trial)]
    public async Task PossuiModuloPorEstabelecimentoAsync_DeveRetornarFalse_QuandoAssinaturaNaoEstaAtiva(
        AssinaturaStatus status)
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
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
    public async Task ObterPorProfissionalAutonomoAsync_DeveLiberarModulosDoAutonomo_QuandoAssinaturaAtiva()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorProfissionalAutonomoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 3,
                PlanoId = 4,
                ProfissionalAutonomoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano
                {
                    Id = 4,
                    Nome = "Plano Solo",
                    LimiteProfissionais = 1,
                    LimiteServicos = 15,
                    LimiteAgendamentos = 200
                }
            });

        var service = CreateService();

        var resultado = await service.ObterPorProfissionalAutonomoAsync(20);

        Assert.True(resultado.AssinaturaAtiva);
        Assert.Equal("ProfissionalAutonomo", resultado.TipoAssinatura);
        Assert.Equal(20, resultado.ProfissionalAutonomoId);
        Assert.Contains("ProfissionalAutonomo", resultado.Modulos);
        Assert.Contains("Servicos", resultado.Modulos);
        Assert.DoesNotContain("Profissionais", resultado.Modulos);
        Assert.Equal(1, resultado.Limites.Profissionais);
        Assert.Equal(15, resultado.Limites.Servicos);
        Assert.Equal(200, resultado.Limites.Agendamentos);
    }

    [Fact]
    public async Task PossuiModuloPorProfissionalAutonomoAsync_DeveRetornarTrue_QuandoModuloEstaLiberado()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorProfissionalAutonomoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 3,
                ProfissionalAutonomoId = 20,
                Status = AssinaturaStatus.Ativa,
                Plano = new Plano { Id = 4, Nome = "Plano Solo" }
            });

        var service = CreateService();

        var possuiModulo = await service.PossuiModuloPorProfissionalAutonomoAsync(
            20,
            ModuloAssinatura.Agenda);

        Assert.True(possuiModulo);
    }

    [Fact]
    public async Task ObterPorProfissionalAutonomoAsync_DeveRetornarBloqueado_QuandoNaoExistirAssinatura()
    {
        _assinaturaRepository
            .Setup(r => r.ObterAtualPorProfissionalAutonomoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Assinatura?)null);

        var service = CreateService();

        var resultado = await service.ObterPorProfissionalAutonomoAsync(20);

        Assert.False(resultado.AssinaturaAtiva);
        Assert.Null(resultado.AssinaturaId);
        Assert.Null(resultado.Status);
        Assert.Empty(resultado.Modulos);
        Assert.Null(resultado.Limites.Profissionais);
        Assert.Null(resultado.Limites.Servicos);
        Assert.Null(resultado.Limites.Agendamentos);
    }

    private ModulosAssinaturaService CreateService() =>
        new(_assinaturaRepository.Object);
}
