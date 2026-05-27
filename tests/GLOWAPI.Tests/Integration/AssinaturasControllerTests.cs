using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class AssinaturasControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AssinaturasControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Iniciar_SemToken_DeveRetornar401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = 1,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            estabelecimentoId = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_DeveCriarAssinaturaPendente_ParaEstabelecimento()
    {
        var seed = await SeedEstabelecimentoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            estabelecimentoId = seed.EstabelecimentoId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        Assert.Equal(seed.PlanoId, data.GetProperty("planoId").GetInt32());
        Assert.Equal(seed.EstabelecimentoId, data.GetProperty("estabelecimentoId").GetInt32());
        Assert.Equal("PendentePagamento", data.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(db.Assinaturas.Where(assinatura => assinatura.EstabelecimentoId == seed.EstabelecimentoId));
    }

    private async Task<(string Email, string Senha, int PlanoId, int EstabelecimentoId)> SeedEstabelecimentoAsync()
    {
        const string email = "assinatura-estabelecimento@email.com";
        const string senha = "Senha123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<GLOWAPI.Application.Interfaces.Services.IPasswordHasher>();

        db.Assinaturas.RemoveRange(db.Assinaturas);
        db.EstabelecimentoUsuarios.RemoveRange(db.EstabelecimentoUsuarios);
        db.Estabelecimentos.RemoveRange(db.Estabelecimentos);
        db.Planos.RemoveRange(db.Planos);
        db.Usuarios.RemoveRange(db.Usuarios.Where(usuario => usuario.Email == email));

        var usuario = new Usuario
        {
            Nome = "Dono Estabelecimento",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.DonoEstabelecimento,
            Ativo = true
        };

        var plano = new Plano
        {
            Nome = "Plano Assinatura Teste",
            Descricao = "Plano para teste de assinatura",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            LimiteProfissionais = 5,
            LimiteServicos = 20,
            LimiteAgendamentos = 200,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = "Estabelecimento Teste",
            Descricao = "Estabelecimento para assinatura",
            Telefone = "11999999999",
            Email = "loja@email.com",
            Ativo = true
        };

        db.Usuarios.Add(usuario);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.Add(new EstabelecimentoUsuario
        {
            UsuarioId = usuario.Id,
            EstabelecimentoId = estabelecimento.Id,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        });

        await db.SaveChangesAsync();

        return (email, senha, plano.Id, estabelecimento.Id);
    }

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = loginBody.GetProperty("data").GetProperty("token").GetString();
        client.DefaultRequestHeaders.Add("x-glow-token", token);
    }
}
