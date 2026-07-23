using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
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
                Descricao = "Plano de entrada para autonomos",
                Preco = 29.99m,
                Periodo = PlanoPeriodo.Mensal,
                LimiteProfissionais = 1,
                LimiteServicos = 10,
                LimiteAgendamentos = null,
                Ativo = true
            }
        };

        var repository = new Mock<IPlanoRepository>();
        repository
            .Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(planos);

        var promocao = new Mock<IPromocaoLancamentoService>();
        promocao
            .Setup(s => s.ObterStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromocaoLancamentoStatusDto(true, 50, 30, 50, 7, 7, 10));

        var service = new PlanoService(repository.Object, promocao.Object);

        var resultado = await service.ListarAtivosAsync();
        var plano = resultado.Planos.Single();

        Assert.Equal("Basic", plano.Nome);
        Assert.Equal("Plano de entrada para autonomos", plano.Descricao);
        Assert.Equal(29.99m, plano.Preco);
        Assert.Equal("Mensal", plano.Periodo);
        Assert.Equal(1, plano.LimiteProfissionais);
        Assert.Equal(10, plano.LimiteServicos);
        Assert.Null(plano.LimiteAgendamentos);
        Assert.Equal(1, plano.LimiteUsuarios);
        Assert.Null(plano.LimiteAgendamentosPorDia);
        Assert.False(plano.PrioridadeListagemPublica);
        Assert.Contains("Agenda", plano.Modulos);
        Assert.Contains("Servicos", plano.Modulos);
        Assert.Contains("HorariosAtendimento", plano.Modulos);
        Assert.Contains("Notificacoes", plano.Modulos);
        Assert.Contains("Email", plano.Modulos);
        Assert.DoesNotContain("WhatsApp", plano.Modulos);
        Assert.DoesNotContain("Caixa", plano.Modulos);
        Assert.Contains("Agenda simples", plano.Funcionalidades);
        Assert.True(resultado.PromocaoLancamento.Disponivel);
        Assert.Equal(50, resultado.PromocaoLancamento.VagasRestantes);

        repository.Verify(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()), Times.Once);
        promocao.Verify(s => s.ObterStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
