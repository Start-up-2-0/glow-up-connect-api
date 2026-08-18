using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class ModulosAssinaturaAcessoTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ModulosAssinaturaAcessoTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenDeOutroTenant_DeveReceber403SemVinculo()
    {
        var lojaA = await SeedLojaAsync("Premium", TipoAssinatura.Estabelecimento);
        var lojaB = await SeedLojaAsync("Premium", TipoAssinatura.Estabelecimento);

        var client = _factory.CreateClient();
        await AutenticarAsync(client, lojaA.OwnerEmail, lojaA.Senha);

        var response = await client.GetAsync($"/api/estabelecimentos/{lojaB.EstabelecimentoId}/agenda");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("USUARIO_SEM_VINCULO_NEGOCIO", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PlanoEssencial_DeveBloquearFinanceiroEClientes()
    {
        var loja = await SeedLojaAsync("Essencial", TipoAssinatura.Estabelecimento);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, loja.OwnerEmail, loja.Senha);

        var financeiro = await client.GetAsync($"/api/estabelecimentos/{loja.EstabelecimentoId}/financeiro/dashboard");
        var clientes = await client.GetAsync($"/api/estabelecimentos/{loja.EstabelecimentoId}/clientes");

        Assert.Equal(HttpStatusCode.Forbidden, financeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clientes.StatusCode);

        var financeiroBody = await financeiro.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("SUBSCRIPTION_MODULE_BLOCKED", financeiroBody.GetProperty("code").GetString());

        var clientesBody = await clientes.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("SUBSCRIPTION_MODULE_BLOCKED", clientesBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AutonomoEssencial_DeveBloquearEquipeProfissionais()
    {
        var loja = await SeedLojaAsync("Essencial", TipoAssinatura.ProfissionalAutonomo);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, loja.OwnerEmail, loja.Senha);

        var response = await client.GetAsync($"/api/estabelecimentos/{loja.EstabelecimentoId}/equipe/membros");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("SUBSCRIPTION_MODULE_BLOCKED", body.GetProperty("code").GetString());
    }

    private async Task<SeedLoja> SeedLojaAsync(string nomePlano, TipoAssinatura tipoAssinatura)
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = new Usuario
        {
            Nome = "Owner Modulos",
            Email = $"owner-{sufixo}@email.com",
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = tipoAssinatura == TipoAssinatura.ProfissionalAutonomo
                ? UserRole.ProfissionalAutonomo
                : UserRole.DonoEstabelecimento,
            Ativo = true
        };

        var plano = new Plano
        {
            Nome = $"{nomePlano} {sufixo}",
            Descricao = "Plano para teste de modulo",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Loja {sufixo}",
            Descricao = "Loja teste modulo",
            Telefone = "11999999999",
            Email = $"loja-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.Add(owner);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.Add(new EstabelecimentoUsuario
        {
            EstabelecimentoId = estabelecimento.Id,
            UsuarioId = owner.Id,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        });

        db.Assinaturas.Add(new Assinatura
        {
            EstabelecimentoId = estabelecimento.Id,
            PlanoId = plano.Id,
            TipoAssinatura = tipoAssinatura,
            DataReferenciaCiclo = DateTime.UtcNow.Date,
            DiaVencimento = DateTime.UtcNow.Day,
            Status = AssinaturaStatus.Ativa,
            Inicio = DateTime.UtcNow.AddDays(-1),
            Fim = DateTime.UtcNow.AddMonths(1)
        });

        await db.SaveChangesAsync();

        return new SeedLoja(estabelecimento.Id, owner.Email, senha);
    }

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();
    }

    private sealed record SeedLoja(int EstabelecimentoId, string OwnerEmail, string Senha);
}
