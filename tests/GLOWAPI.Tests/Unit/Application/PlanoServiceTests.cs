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
                Nome = "Basic",
                Descricao = "Plano gratuito para autonomos iniciando",
                Preco = 0m,
                Periodo = PlanoPeriodo.Mensal,
                LimiteProfissionais = 1,
                LimiteServicos = 10,
                LimiteAgendamentos = 10,
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
        Assert.Equal("Basic", resultado[0].Nome);
        Assert.Equal("Plano gratuito para autonomos iniciando", resultado[0].Descricao);
        Assert.Equal(0m, resultado[0].Preco);
        Assert.Equal("Mensal", resultado[0].Periodo);
        Assert.Equal(1, resultado[0].LimiteProfissionais);
        Assert.Equal(10, resultado[0].LimiteServicos);
        Assert.Equal(10, resultado[0].LimiteAgendamentos);
        Assert.Equal(1, resultado[0].LimiteUsuarios);
        Assert.Equal(10, resultado[0].LimiteAgendamentosPorDia);
        Assert.False(resultado[0].PrioridadeListagemPublica);
        Assert.Contains("Agenda", resultado[0].Modulos);
        Assert.Contains("Servicos", resultado[0].Modulos);
        Assert.Contains("HorariosAtendimento", resultado[0].Modulos);
        Assert.Contains("Notificacoes", resultado[0].Modulos);
        Assert.Contains("Email", resultado[0].Modulos);
        Assert.DoesNotContain("WhatsApp", resultado[0].Modulos);
        Assert.DoesNotContain("Caixa", resultado[0].Modulos);
        Assert.Contains("Agenda simples", resultado[0].Funcionalidades);

        repository.Verify(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
