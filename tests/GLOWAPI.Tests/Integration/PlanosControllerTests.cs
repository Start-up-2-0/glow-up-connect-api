using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class PlanosControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PlanosControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListarAtivos_DeveRetornarApenasPlanosAtivos_SemAutenticacao()
    {
        await SeedPlanosAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/planos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        var planos = data.GetProperty("planos").EnumerateArray().ToList();
        Assert.Single(planos);

        var promocao = data.GetProperty("promocaoLancamento");
        Assert.Equal(30, promocao.GetProperty("diasTrial").GetInt32());
        Assert.Contains(5, promocao.GetProperty("diasVencimentoPermitidos").EnumerateArray().Select(item => item.GetInt32()));

        var plano = planos[0];
        Assert.Equal("Basic", plano.GetProperty("nome").GetString());
        Assert.Equal("Mensal", plano.GetProperty("periodo").GetString());
        Assert.Equal(1, plano.GetProperty("limiteProfissionais").GetInt32());
        Assert.Equal(10, plano.GetProperty("limiteServicos").GetInt32());
        Assert.Equal(10, plano.GetProperty("limiteAgendamentos").GetInt32());
        Assert.Equal(1, plano.GetProperty("limiteUsuarios").GetInt32());
        Assert.Equal(10, plano.GetProperty("limiteAgendamentosPorDia").GetInt32());
        Assert.False(plano.GetProperty("prioridadeListagemPublica").GetBoolean());

        var modulos = plano.GetProperty("modulos").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Contains("Agenda", modulos);
        Assert.Contains("Servicos", modulos);
        Assert.Contains("HorariosAtendimento", modulos);
        Assert.Contains("Notificacoes", modulos);
        Assert.Contains("Email", modulos);
        Assert.DoesNotContain("WhatsApp", modulos);
        Assert.DoesNotContain("Caixa", modulos);
    }

    private async Task SeedPlanosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Planos.RemoveRange(db.Planos);
        db.Planos.AddRange(
            new Plano
            {
                Nome = "Basic",
                Descricao = "Plano de entrada para autonomos",
                Preco = 29.99m,
                Periodo = PlanoPeriodo.Mensal,
                LimiteProfissionais = 1,
                LimiteServicos = 10,
                LimiteAgendamentos = 10,
                Ativo = true
            },
            new Plano
            {
                Nome = "Plano Inativo",
                Descricao = "Nao deve aparecer",
                Preco = 199.90m,
                Periodo = PlanoPeriodo.Anual,
                LimiteProfissionais = 10,
                LimiteServicos = 50,
                LimiteAgendamentos = 500,
                Ativo = false
            });

        await db.SaveChangesAsync();
    }
}
