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

        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.Single(data);

        var plano = data[0];
        Assert.Equal("Plano Ativo", plano.GetProperty("nome").GetString());
        Assert.Equal("Mensal", plano.GetProperty("periodo").GetString());
        Assert.Equal(5, plano.GetProperty("limiteProfissionais").GetInt32());
        Assert.Equal(20, plano.GetProperty("limiteServicos").GetInt32());
        Assert.Equal(200, plano.GetProperty("limiteAgendamentos").GetInt32());
        Assert.Empty(plano.GetProperty("modulos").EnumerateArray());
    }

    private async Task SeedPlanosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Planos.RemoveRange(db.Planos);
        db.Planos.AddRange(
            new Plano
            {
                Nome = "Plano Ativo",
                Descricao = "Disponivel para contratacao",
                Preco = 99.90m,
                Periodo = PlanoPeriodo.Mensal,
                LimiteProfissionais = 5,
                LimiteServicos = 20,
                LimiteAgendamentos = 200,
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
