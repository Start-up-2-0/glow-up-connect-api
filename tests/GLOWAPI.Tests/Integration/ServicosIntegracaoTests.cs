using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class ServicosIntegracaoTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ServicosIntegracaoTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_DeveCriarServicoSemProfissional_EListarNaAdministracao()
    {
        var seed = await SeedServicosAsync(limiteServicos: 5);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var criar = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos",
            new
            {
                nome = "Barba",
                descricao = "Barba completa",
                precoBase = 35,
                duracaoMinutos = 30
            });

        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);

        var listar = await client.GetAsync($"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos");
        Assert.Equal(HttpStatusCode.OK, listar.StatusCode);

        var body = await listar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("data").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Recepcionista_DeveSerBloqueadaAoCriarServico()
    {
        var seed = await SeedServicosAsync(limiteServicos: 5);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.RecepcionistaEmail, seed.Senha);

        var criar = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos",
            new
            {
                nome = "Sobrancelha",
                precoBase = 25,
                duracaoMinutos = 20
            });

        Assert.Equal(HttpStatusCode.Forbidden, criar.StatusCode);
    }

    [Fact]
    public async Task ServicoSemProfissional_NaoDeveAparecerNaListagemPublica()
    {
        var seed = await SeedServicosAsync(limiteServicos: 5);
        var ownerClient = _factory.CreateClient();
        await AutenticarAsync(ownerClient, seed.OwnerEmail, seed.Senha);

        await ownerClient.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos",
            new
            {
                nome = "Servico Sem Profissional",
                precoBase = 40,
                duracaoMinutos = 30
            });

        var publicClient = _factory.CreateClient();
        var listarPublico = await publicClient.GetAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}/servicos");

        Assert.Equal(HttpStatusCode.OK, listarPublico.StatusCode);
        var body = await listarPublico.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("data").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Owner_DeveSerBloqueadoAoExcederLimiteDeServicos()
    {
        var seed = await SeedServicosAsync(limiteServicos: 1);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var primeiro = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos",
            new { nome = "Servico 1", precoBase = 10, duracaoMinutos = 30 });
        var segundo = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/servicos",
            new { nome = "Servico 2", precoBase = 20, duracaoMinutos = 30 });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, segundo.StatusCode);
    }

    private async Task<SeedServicos> SeedServicosAsync(int? limiteServicos)
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = CriarUsuario($"owner-servico-{sufixo}@email.com", senha, hasher);
        var recepcionista = CriarUsuario($"recepcao-servico-{sufixo}@email.com", senha, hasher);

        var plano = new Plano
        {
            Nome = $"Plano Servicos {sufixo}",
            Descricao = "Plano para testes de servicos",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            LimiteServicos = limiteServicos,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Negocio Servicos {sufixo}",
            Descricao = "Negocio para testes integrados de servicos",
            Telefone = "11999999999",
            Email = $"negocio-servico-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.AddRange(owner, recepcionista);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.AddRange(
            CriarVinculoUsuario(estabelecimento.Id, owner.Id, EstablishmentUserRole.Owner),
            CriarVinculoUsuario(estabelecimento.Id, recepcionista.Id, EstablishmentUserRole.Receptionist));

        db.Assinaturas.Add(new Assinatura
        {
            EstabelecimentoId = estabelecimento.Id,
            PlanoId = plano.Id,
            Status = AssinaturaStatus.Ativa,
            Inicio = DateTime.UtcNow.AddDays(-1),
            Fim = DateTime.UtcNow.AddMonths(1),
            Gateway = GatewayPagamento.MercadoPago
        });

        await db.SaveChangesAsync();

        return new SeedServicos(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            owner.Email,
            recepcionista.Email,
            senha);
    }

    private static Usuario CriarUsuario(string email, string senha, IPasswordHasher hasher) =>
        new()
        {
            Nome = "Usuario Servicos",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.Cliente,
            Ativo = true
        };

    private static EstabelecimentoUsuario CriarVinculoUsuario(
        int estabelecimentoId,
        int usuarioId,
        EstablishmentUserRole role) =>
        new()
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            RoleNoEstabelecimento = role,
            Ativo = true
        };

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();
        // Sessão autenticada via cookie HttpOnly guc_access (HandleCookies no client).
    }

    private record SeedServicos(
        int EstabelecimentoId,
        Guid PublicGuid,
        string OwnerEmail,
        string RecepcionistaEmail,
        string Senha);
}
