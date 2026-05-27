using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class PlanoServiceTests
{
    [Fact]
    public async Task ListarAtivosAsync_DeveRetornarPlanosMapeados()
    {
        var planos = new List<Plano>
        {
            new()
            {
                Id = 1,
                Nome = "Basico",
                Descricao = "Plano inicial",
                Preco = 49.90m,
                Periodo = PlanoPeriodo.Mensal,
                LimiteProfissionais = 3,
                LimiteServicos = 10,
                LimiteAgendamentos = 100,
                Ativo = true
            }
        };

        var repository = new Mock<IPlanoRepository>();
        repository
            .Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(planos);

        var service = new PlanoService(repository.Object);

        var resultado = await service.ListarAtivosAsync();

        Assert.Single(resultado);
        Assert.Equal("Basico", resultado[0].Nome);
        Assert.Equal("Plano inicial", resultado[0].Descricao);
        Assert.Equal(49.90m, resultado[0].Preco);
        Assert.Equal("Mensal", resultado[0].Periodo);
        Assert.Equal(3, resultado[0].LimiteProfissionais);
        Assert.Equal(10, resultado[0].LimiteServicos);
        Assert.Equal(100, resultado[0].LimiteAgendamentos);
        Assert.Empty(resultado[0].Modulos);

        repository.Verify(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
