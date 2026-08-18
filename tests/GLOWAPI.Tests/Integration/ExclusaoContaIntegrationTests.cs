using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class ExclusaoContaIntegrationTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ExclusaoContaIntegrationTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_SolicitarExclusao_DeveSuspenderAssinaturaEBloquearLogin()
    {
        var seed = await SeedOwnerAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var exclusao = await client.PostAsJsonAsync(
            "/api/privacidade/solicitar-exclusao",
            new { senha = seed.Senha });
        Assert.Equal(HttpStatusCode.OK, exclusao.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var assinatura = db.Assinaturas.Single(a => a.EstabelecimentoId == seed.EstabelecimentoId);
            var estabelecimento = db.Estabelecimentos.Single(e => e.Id == seed.EstabelecimentoId);
            Assert.Equal(AssinaturaStatus.Suspensa, assinatura.Status);
            Assert.False(estabelecimento.VisivelPublicamente);
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = seed.Email, senha = seed.Senha });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("CONTA_EM_EXCLUSAO", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("details").TryGetProperty("reativarAte", out _));
    }

    [Fact]
    public async Task ReativarDentroDoPrazo_DeveRestaurarAssinaturaEPermitirLogin()
    {
        var seed = await SeedOwnerAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);
        (await client.PostAsJsonAsync("/api/privacidade/solicitar-exclusao", new { senha = seed.Senha }))
            .EnsureSuccessStatusCode();

        var reativar = await client.PostAsJsonAsync(
            "/api/auth/reativar-conta",
            new { email = seed.Email, senha = seed.Senha });
        Assert.Equal(HttpStatusCode.OK, reativar.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var assinatura = db.Assinaturas.Single(a => a.EstabelecimentoId == seed.EstabelecimentoId);
        var estabelecimento = db.Estabelecimentos.Single(e => e.Id == seed.EstabelecimentoId);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
        Assert.True(estabelecimento.VisivelPublicamente);
    }

    [Fact]
    public async Task WorkerAposPrazo_DeveAnonimizarEEncerrar()
    {
        var seed = await SeedOwnerAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);
        (await client.PostAsJsonAsync("/api/privacidade/solicitar-exclusao", new { senha = seed.Senha }))
            .EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var usuario = db.Usuarios.Single(u => u.Email == seed.Email);
            usuario.ExclusaoEfetivarEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var exclusao = scope.ServiceProvider.GetRequiredService<IExclusaoContaService>();
            var efetivadas = await exclusao.EfetivarVencidasAsync();
            Assert.Equal(1, efetivadas);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == seed.UsuarioId);
            var assinatura = db.Assinaturas.Single(a => a.EstabelecimentoId == seed.EstabelecimentoId);
            Assert.Equal(ExclusaoStatus.Concluida, usuario.ExclusaoStatus);
            Assert.False(usuario.Ativo);
            Assert.StartsWith("deleted+", usuario.Email);
            Assert.Equal(AssinaturaStatus.Cancelada, assinatura.Status);
        }

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"deleted+{seed.UsuarioId}@invalid.local", senha = seed.Senha });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("USER_INACTIVE", loginBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProfissionalDaEquipe_NaoDeveSuspenderAssinaturaDoOwner()
    {
        var seed = await SeedOwnerComProfissionalAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.ProfissionalEmail, seed.Senha);

        var exclusao = await client.PostAsJsonAsync(
            "/api/privacidade/solicitar-exclusao",
            new { senha = seed.Senha });
        Assert.Equal(HttpStatusCode.OK, exclusao.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var assinatura = db.Assinaturas.Single(a => a.EstabelecimentoId == seed.EstabelecimentoId);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura.Status);
    }

    [Fact]
    public async Task CadastroComMesmoEmailDurantePendencia_DeveRetornar409()
    {
        var seed = await SeedOwnerAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);
        (await client.PostAsJsonAsync("/api/privacidade/solicitar-exclusao", new { senha = seed.Senha }))
            .EnsureSuccessStatusCode();

        var cadastro = await client.PostAsJsonAsync("/api/usuario", new
        {
            nome = "Outro",
            email = seed.Email,
            telefone = "11988887777",
            senha = "Senha123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, cadastro.StatusCode);
    }

    private async Task<SeedOwner> SeedOwnerAsync()
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = new Usuario
        {
            Nome = "Owner Exclusao",
            Email = $"owner-exc-{sufixo}@email.com",
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.DonoEstabelecimento,
            Ativo = true
        };
        var plano = new Plano
        {
            Nome = $"Essencial {sufixo}",
            Preco = 49.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };
        var estabelecimento = new Estabelecimento
        {
            Nome = $"Loja {sufixo}",
            Telefone = "11999999999",
            Email = $"loja-{sufixo}@email.com",
            Ativo = true,
            VisivelPublicamente = true
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
            TipoAssinatura = TipoAssinatura.Estabelecimento,
            Status = AssinaturaStatus.Ativa,
            DataReferenciaCiclo = DateTime.UtcNow.Date,
            DiaVencimento = 10,
            Inicio = DateTime.UtcNow.AddDays(-1),
            Fim = DateTime.UtcNow.AddMonths(1),
            RenovacaoAutomatica = true
        });
        await db.SaveChangesAsync();

        return new SeedOwner(owner.Id, estabelecimento.Id, owner.Email, senha);
    }

    private async Task<SeedEquipe> SeedOwnerComProfissionalAsync()
    {
        var owner = await SeedOwnerAsync();
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var profissionalUsuario = new Usuario
        {
            Nome = "Profissional Equipe",
            Email = $"prof-exc-{sufixo}@email.com",
            Telefone = "11988887777",
            Senha = hasher.Hash(senha),
            Role = UserRole.ProfissionalEstabelecimento,
            Ativo = true
        };
        db.Usuarios.Add(profissionalUsuario);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.Add(new EstabelecimentoUsuario
        {
            EstabelecimentoId = owner.EstabelecimentoId,
            UsuarioId = profissionalUsuario.Id,
            RoleNoEstabelecimento = EstablishmentUserRole.Profissional,
            Ativo = true
        });
        await db.SaveChangesAsync();

        return new SeedEquipe(owner.EstabelecimentoId, profissionalUsuario.Email, senha);
    }

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();
    }

    private sealed record SeedOwner(int UsuarioId, int EstabelecimentoId, string Email, string Senha);
    private sealed record SeedEquipe(int EstabelecimentoId, string ProfissionalEmail, string Senha);
}
