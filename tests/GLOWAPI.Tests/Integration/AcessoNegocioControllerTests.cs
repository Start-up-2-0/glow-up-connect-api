using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class AcessoNegocioControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AcessoNegocioControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_DeveAcessarCaixaDoNegocio()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var response = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/caixa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cliente_DeveReceberListaVaziaDeEstabelecimentos()
    {
        const string email = "cliente-acesso@email.com";
        const string senha = "Senha123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            db.Usuarios.RemoveRange(db.Usuarios.Where(usuario => usuario.Email == email));
            db.Usuarios.Add(new Usuario
            {
                Nome = "Cliente Acesso",
                Email = email,
                Telefone = "11999999999",
                Senha = hasher.Hash(senha),
                Role = UserRole.Cliente,
                Ativo = true
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        await AutenticarAsync(client, email, senha);

        var response = await client.GetAsync("/api/usuario/me/estabelecimentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task MeEstabelecimentos_DeveRetornarApenasNegociosComVinculoAtivo()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.ProfissionalEmail, seed.Senha);

        var response = await client.GetAsync("/api/usuario/me/estabelecimentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var item = Assert.Single(body.EnumerateArray());
        Assert.Equal(seed.EstabelecimentoId, item.GetProperty("estabelecimentoId").GetInt32());
        Assert.Equal("Profissional", item.GetProperty("role").GetString());
        Assert.True(item.GetProperty("possuiVinculoProfissional").GetBoolean());
        Assert.Contains(item.GetProperty("permissoes").EnumerateArray(), permissao =>
            permissao.GetString() == "AgendaVisualizarPropria");
        Assert.Contains(item.GetProperty("modulos").EnumerateArray(), modulo =>
            modulo.GetString() == "Profissionais");
    }

    [Fact]
    public async Task Admin_DeveGerenciarEquipe()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.AdminEmail, seed.Senha);

        var response = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/usuarios",
            new
            {
                email = seed.NovoUsuarioEmail,
                role = EstablishmentUserRole.Receptionist
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Contains(db.EstabelecimentoUsuarios, vinculo =>
            vinculo.EstabelecimentoId == seed.EstabelecimentoId
            && vinculo.UsuarioId == seed.NovoUsuarioId
            && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Receptionist
            && vinculo.Ativo);
    }

    [Fact]
    public async Task Admin_PatchRoleUsuario_DeveAlterarCargo()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.AdminEmail, seed.Senha);

        int recepcionistaUsuarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            recepcionistaUsuarioId = db.Usuarios
                .Single(usuario => usuario.Email == seed.RecepcionistaEmail)
                .Id;
        }

        var response = await PatchAsJsonAsync(
            client,
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/usuarios/{recepcionistaUsuarioId}/role",
            new { role = EstablishmentUserRole.Manager });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        Assert.Equal("Manager", data.GetProperty("role").GetString());
        Assert.Equal(recepcionistaUsuarioId, data.GetProperty("usuarioId").GetInt32());
    }

    [Fact]
    public async Task Admin_PatchStatusUsuario_DeveInativarEReativar()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.AdminEmail, seed.Senha);

        int recepcionistaUsuarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            recepcionistaUsuarioId = db.Usuarios
                .Single(usuario => usuario.Email == seed.RecepcionistaEmail)
                .Id;
        }

        var responseInativar = await PatchAsJsonAsync(
            client,
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/usuarios/{recepcionistaUsuarioId}/status",
            new { ativo = false });

        Assert.Equal(HttpStatusCode.OK, responseInativar.StatusCode);

        var inativarBody = await responseInativar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.False(inativarBody.GetProperty("data").GetProperty("ativo").GetBoolean());

        var responseReativar = await PatchAsJsonAsync(
            client,
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/usuarios/{recepcionistaUsuarioId}/status",
            new { ativo = true });

        Assert.Equal(HttpStatusCode.OK, responseReativar.StatusCode);

        var reativarBody = await responseReativar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(reativarBody.GetProperty("data").GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Admin_PatchStatusProfissional_DeveAtualizarAtivoEAgendamento()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.AdminEmail, seed.Senha);

        int profissionalId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            profissionalId = db.Profissionais
                .Single(profissional => profissional.Email == seed.ProfissionalEmail)
                .Id;
        }

        var response = await PatchAsJsonAsync(
            client,
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/profissionais/{profissionalId}/status",
            new { ativo = false, podeReceberAgendamento = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        Assert.False(data.GetProperty("ativo").GetBoolean());
        Assert.False(data.GetProperty("podeReceberAgendamento").GetBoolean());
        Assert.Equal(profissionalId, data.GetProperty("profissionalId").GetInt32());
    }

    [Fact]
    public async Task Recepcionista_PatchRoleUsuario_DeveRetornar403()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.RecepcionistaEmail, seed.Senha);

        int recepcionistaUsuarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            recepcionistaUsuarioId = db.Usuarios
                .Single(usuario => usuario.Email == seed.RecepcionistaEmail)
                .Id;
        }

        var response = await PatchAsJsonAsync(
            client,
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/equipe/usuarios/{recepcionistaUsuarioId}/role",
            new { role = EstablishmentUserRole.Manager });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Recepcionista_DeveVisualizarAgendaGeralESerBloqueadaNoCaixa()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.RecepcionistaEmail, seed.Senha);

        var agenda = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/agenda");
        var caixa = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/caixa");

        Assert.Equal(HttpStatusCode.OK, agenda.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, caixa.StatusCode);
    }

    [Fact]
    public async Task Profissional_DeveVisualizarApenasAgendaPropriaESerBloqueadoNoCaixa()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.ProfissionalEmail, seed.Senha);

        var agendaPropria = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/agenda/propria");
        var caixa = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/caixa");

        Assert.Equal(HttpStatusCode.OK, agendaPropria.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, caixa.StatusCode);
    }

    [Fact]
    public async Task UsuarioSemVinculo_DeveSerBloqueadoNoNegocio()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.SemVinculoEmail, seed.Senha);

        var response = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/agenda");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("USUARIO_SEM_VINCULO_NEGOCIO", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UsuarioComVinculoInativo_DeveSerBloqueadoNoNegocio()
    {
        var seed = await SeedAcessoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.InativoEmail, seed.Senha);

        var response = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/agenda");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("USUARIO_SEM_VINCULO_NEGOCIO", body.GetProperty("code").GetString());
    }

    private async Task<SeedAcesso> SeedAcessoAsync()
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = CriarUsuario($"owner-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);
        var admin = CriarUsuario($"admin-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);
        var recepcionista = CriarUsuario($"recepcao-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);
        var profissionalUsuario = CriarUsuario($"profissional-{sufixo}@email.com", senha, hasher, UserRole.ProfissionalEstabelecimento);
        var semVinculo = CriarUsuario($"sem-vinculo-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);
        var inativo = CriarUsuario($"inativo-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);
        var novoUsuario = CriarUsuario($"novo-{sufixo}@email.com", senha, hasher, UserRole.DonoEstabelecimento);

        var plano = new Plano
        {
            Nome = $"Premium {sufixo}",
            Descricao = "Plano premium para teste de acesso",
            Preco = 199.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Negocio {sufixo}",
            Descricao = "Negocio para teste integrado de acesso",
            Telefone = "11999999999",
            Email = $"negocio-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.AddRange(owner, admin, recepcionista, profissionalUsuario, semVinculo, inativo, novoUsuario);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        var profissional = new Profissional
        {
            UsuarioId = profissionalUsuario.Id,
            NomePublico = "Profissional Teste",
            Email = profissionalUsuario.Email,
            Telefone = profissionalUsuario.Telefone,
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        db.Profissionais.Add(profissional);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.AddRange(
            CriarVinculoUsuario(estabelecimento.Id, owner.Id, EstablishmentUserRole.Owner, ativo: true),
            CriarVinculoUsuario(estabelecimento.Id, admin.Id, EstablishmentUserRole.Admin, ativo: true),
            CriarVinculoUsuario(estabelecimento.Id, recepcionista.Id, EstablishmentUserRole.Receptionist, ativo: true),
            CriarVinculoUsuario(estabelecimento.Id, profissionalUsuario.Id, EstablishmentUserRole.Profissional, ativo: true),
            CriarVinculoUsuario(estabelecimento.Id, inativo.Id, EstablishmentUserRole.Admin, ativo: false));

        db.ProfissionalEstabelecimentos.Add(new ProfissionalEstabelecimento
        {
            EstabelecimentoId = estabelecimento.Id,
            ProfissionalId = profissional.Id,
            Ativo = true,
            PodeReceberAgendamento = true
        });

        db.Assinaturas.Add(new Assinatura
        {
            EstabelecimentoId = estabelecimento.Id,
            PlanoId = plano.Id,
            DiaVencimento = 10,
            Status = AssinaturaStatus.Ativa,
            Inicio = DateTime.UtcNow.AddDays(-1),
            Fim = DateTime.UtcNow.AddMonths(1)
        });

        db.Caixas.Add(new Caixa
        {
            EstabelecimentoId = estabelecimento.Id,
            SaldoTotal = 1000,
            SaldoDisponivel = 900,
            SaldoRetido = 100
        });

        await db.SaveChangesAsync();

        return new SeedAcesso(
            estabelecimento.Id,
            owner.Email,
            admin.Email,
            recepcionista.Email,
            profissionalUsuario.Email,
            semVinculo.Email,
            inativo.Email,
            novoUsuario.Email,
            novoUsuario.Id,
            senha);
    }

    private static Usuario CriarUsuario(string email, string senha, IPasswordHasher hasher, UserRole role) =>
        new()
        {
            Nome = "Usuario Acesso",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = role,
            Ativo = true
        };

    private static EstabelecimentoUsuario CriarVinculoUsuario(
        int estabelecimentoId,
        int usuarioId,
        EstablishmentUserRole role,
        bool ativo) =>
        new()
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            RoleNoEstabelecimento = role,
            Ativo = ativo
        };

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = loginBody.GetProperty("data").GetProperty("token").GetString();
        client.DefaultRequestHeaders.Add("x-glow-token", token);
    }

    private static Task<HttpResponseMessage> PatchAsJsonAsync(
        HttpClient client,
        string requestUri,
        object payload) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = JsonContent.Create(payload),
        });

    private record SeedAcesso(
        int EstabelecimentoId,
        string OwnerEmail,
        string AdminEmail,
        string RecepcionistaEmail,
        string ProfissionalEmail,
        string SemVinculoEmail,
        string InativoEmail,
        string NovoUsuarioEmail,
        int NovoUsuarioId,
        string Senha);
}
