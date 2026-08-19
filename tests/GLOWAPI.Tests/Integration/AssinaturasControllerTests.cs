using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using GLOWAPI.Tests.Helpers;
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
    public async Task ObterContextoOnboarding_SemToken_DeveRetornar401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/assinaturas/onboarding/contexto");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ObterContextoOnboarding_ClienteSemEstabelecimento_DeveSugerirCadastro()
    {
        var seed = await SeedUsuarioClienteAsync("cliente-sem-loja@email.com");
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.GetAsync("/api/assinaturas/onboarding/contexto");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        Assert.False(data.GetProperty("temEstabelecimentoProprio").GetBoolean());
        Assert.Equal("CadastrarEstabelecimento", data.GetProperty("proximaEtapa").GetString());
    }

    [Fact]
    public async Task ObterContextoOnboarding_ComEstabelecimentoSemAssinatura_DeveSugerirAssinatura()
    {
        var seed = await SeedEstabelecimentoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.GetAsync("/api/assinaturas/onboarding/contexto");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        Assert.True(data.GetProperty("temEstabelecimentoProprio").GetBoolean());
        Assert.Equal("AssinarPlano", data.GetProperty("proximaEtapa").GetString());
        Assert.Equal(seed.EstabelecimentoId, data.GetProperty("estabelecimentoIdSugerido").GetInt32());
    }

    [Fact]
    public async Task Iniciar_ComEstabelecimentoExistente_DeveBloquearNovoCadastro()
    {
        var seed = await SeedEstabelecimentoAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            pagamento = PagamentoValido(),
            estabelecimento = new
            {
                nome = "Outro Studio",
                descricao = "Duplicado",
                logo = LogoBase64TestHelper.PngDataUri,
                telefone = "11999999999",
                email = "outro@email.com",
                endereco = new
                {
                    cep = "01310100",
                    logradouro = "Rua Glow",
                    numero = "200",
                    bairro = "Centro",
                    cidade = "Sao Paulo",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("ESTABELECIMENTO_ONBOARDING_DUPLICADO", body.GetProperty("code").GetString());
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
            estabelecimentoId = seed.EstabelecimentoId,
            pagamento = PagamentoValido()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        var assinaturaId = data.GetProperty("id").GetInt32();
        Assert.Equal(seed.PlanoId, data.GetProperty("planoId").GetInt32());
        Assert.Equal(seed.EstabelecimentoId, data.GetProperty("estabelecimentoId").GetInt32());
        Assert.Equal("PendentePagamento", data.GetProperty("status").GetString());
        Assert.Equal("Pendente", data.GetProperty("pagamentoInicial").GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("pagamentoInicial").GetProperty("gatewayPaymentId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("pagamentoInicial").GetProperty("checkoutUrl").GetString()));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(db.Assinaturas.Where(assinatura => assinatura.EstabelecimentoId == seed.EstabelecimentoId));
        Assert.Single(db.Pagamentos.Where(pagamento =>
            pagamento.AssinaturaId == assinaturaId
            && pagamento.Status == PagamentoStatus.Pendente));
    }

    [Fact]
    public async Task Iniciar_DeveCriarEstabelecimentoOwnerEAssinaturaPendente()
    {
        var seed = await SeedUsuarioEPlanoAsync("onboarding-estabelecimento@email.com");
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            pagamento = PagamentoValido(),
            estabelecimento = new
            {
                nome = "Studio Glow",
                descricao = "Salao de beleza",
                logo = LogoBase64TestHelper.PngDataUri,
                telefone = "11999999999",
                email = "studio@email.com",
                endereco = new
                {
                    cep = "01310100",
                    logradouro = "Rua Glow",
                    numero = "100",
                    bairro = "Centro",
                    cidade = "Sao Paulo",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        var assinaturaId = data.GetProperty("id").GetInt32();
        var estabelecimentoId = data.GetProperty("estabelecimentoId").GetInt32();

        Assert.Equal(seed.PlanoId, data.GetProperty("planoId").GetInt32());
        Assert.Equal("PendentePagamento", data.GetProperty("status").GetString());
        Assert.True(estabelecimentoId > 0);
        Assert.Equal("Pendente", data.GetProperty("pagamentoInicial").GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var estabelecimento = await db.Estabelecimentos.FindAsync(estabelecimentoId);
        Assert.NotNull(estabelecimento);
        Assert.Equal("Studio Glow", estabelecimento!.Nome);
        Assert.StartsWith("data:image/png;base64,", estabelecimento!.Logo);
        Assert.Equal("5511999999999", estabelecimento.Telefone);
        Assert.Equal("studio@email.com", estabelecimento.Email);
        Assert.NotEqual(Guid.Empty, estabelecimento.PublicGuid);

        Assert.Contains(db.EstabelecimentoUsuarios, vinculo =>
            vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.UsuarioId == seed.UsuarioId
            && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner
            && vinculo.Ativo);

        Assert.Contains(db.Assinaturas, assinatura =>
            assinatura.EstabelecimentoId == estabelecimentoId
            && assinatura.PlanoId == seed.PlanoId
            && assinatura.Status == AssinaturaStatus.PendentePagamento);
        Assert.Contains(db.Pagamentos, pagamento =>
            pagamento.AssinaturaId == assinaturaId
            && pagamento.Status == PagamentoStatus.Pendente);
    }

    [Fact]
    public async Task Iniciar_ComPayloadFrontend_DeveAceitarEnumsComoString()
    {
        var seed = await SeedUsuarioEPlanoAsync("onboarding-frontend-payload@email.com");
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        using var content = new StringContent(
            """
            {
              "planoId": 0,
              "tipoAssinatura": "Estabelecimento",
              "gateway": "MercadoPago",
              "pagamento": {
                "paymentMethodId": "pix"
              },
              "estabelecimento": {
                "nome": "Studio Frontend",
                "descricao": "Salao de beleza",
                "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                "telefone": "11999999999",
                "email": "studio-frontend@email.com",
                "endereco": {
                  "cep": "01310100",
                  "logradouro": "Rua Glow",
                  "numero": "100",
                  "bairro": "Centro",
                  "cidade": "Sao Paulo",
                  "estado": "SP"
                }
              }
            }
            """.Replace("\"planoId\": 0", $"\"planoId\": {seed.PlanoId}"),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/assinaturas", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_DeveCriarProfissionalAutonomoEAssinaturaPendente()
    {
        var seed = await SeedUsuarioEPlanoAsync("onboarding-autonomo@email.com");
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            pagamento = PagamentoValido(),
            profissionalAutonomo = new
            {
                nomePublico = "Maria Glow",
                biografia = "Especialista em beleza",
                logo = LogoBase64TestHelper.PngDataUri,
                telefone = "11999999999",
                email = "maria@email.com",
                endereco = new
                {
                    cep = "13010000",
                    logradouro = "Sala 12",
                    numero = "12",
                    bairro = "Centro",
                    cidade = "Campinas",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        var assinaturaId = data.GetProperty("id").GetInt32();
        var estabelecimentoId = data.GetProperty("estabelecimentoId").GetInt32();

        Assert.Equal(seed.PlanoId, data.GetProperty("planoId").GetInt32());
        Assert.Equal("PendentePagamento", data.GetProperty("status").GetString());
        Assert.True(estabelecimentoId > 0);
        Assert.True(data.GetProperty("profissionalAutonomoId").ValueKind == JsonValueKind.Null);
        Assert.Equal("Pendente", data.GetProperty("pagamentoInicial").GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var profissional = db.Profissionais.SingleOrDefault(item => item.UsuarioId == seed.UsuarioId);
        Assert.NotNull(profissional);
        Assert.Equal(seed.UsuarioId, profissional!.UsuarioId);
        Assert.Equal("Maria Glow", profissional.NomePublico);
        Assert.StartsWith("data:image/png;base64,", profissional!.Logo);
        Assert.Equal("5511999999999", profissional.Telefone);
        Assert.Equal("maria@email.com", profissional.Email);
        Assert.Equal(ProfessionalType.Autonomo, profissional.TipoProfissional);
        Assert.True(profissional.Ativo);
        Assert.NotEqual(Guid.Empty, profissional.PublicGuid);

        Assert.Contains(db.EstabelecimentoUsuarios, vinculo =>
            vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.UsuarioId == seed.UsuarioId
            && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner
            && vinculo.Ativo);
        Assert.Contains(db.ProfissionalEstabelecimentos, vinculo =>
            vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.ProfissionalId == profissional.Id
            && vinculo.Ativo);
        Assert.Contains(db.Assinaturas, assinatura =>
            assinatura.EstabelecimentoId == estabelecimentoId
            && assinatura.PlanoId == seed.PlanoId
            && assinatura.Status == AssinaturaStatus.PendentePagamento);
        Assert.Contains(db.Pagamentos, pagamento =>
            pagamento.AssinaturaId == assinaturaId
            && pagamento.Status == PagamentoStatus.Pendente);
    }

    [Fact]
    public async Task Iniciar_ComEmailPendenteConfirmacao_DeveCriarAssinaturaSemPromoverRole()
    {
        const string email = "onboarding-pendente@email.com";
        const string senha = "Senha123!";
        var planoId = await SeedPlanoPublicoAsync();

        var client = _factory.CreateClient();
        var cadastro = await client.PostAsJsonAsync("/api/usuario", new
        {
            nome = "Dono Onboarding",
            email,
            telefone = "11999999999",
            senha
        });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);

        await AutenticarAsync(client, email, senha);

        var response = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            pagamento = PagamentoValido(),
            estabelecimento = new
            {
                nome = "Studio Pendente",
                descricao = "Salao de beleza",
                logo = LogoBase64TestHelper.PngDataUri,
                telefone = "11999999999",
                email = "studio-pendente@email.com",
                endereco = new
                {
                    cep = "01310100",
                    logradouro = "Rua Glow",
                    numero = "100",
                    bairro = "Centro",
                    cidade = "Sao Paulo",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = body.GetProperty("data");
        Assert.True(data.GetProperty("requerConfirmacaoEmail").GetBoolean());
        Assert.Equal("PendentePagamento", data.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usuario = db.Usuarios.Single(item => item.Email == email);
        Assert.False(usuario.Ativo);
        Assert.Equal(UserRole.Cliente, usuario.Role);
    }

    private async Task<int> SeedPlanoPublicoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await CampanhaPromocionalTestHelper.DesabilitarPromocaoAsync(db);
        await CategoriaEstabelecimentoTestHelper.GarantirCatalogoAsync(db);

        var plano = new Plano
        {
            Nome = $"Plano Onboarding {Guid.NewGuid()}",
            Descricao = "Plano para onboarding pendente",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            LimiteProfissionais = 5,
            LimiteServicos = 20,
            LimiteAgendamentos = 200,
            Ativo = true
        };

        db.Planos.Add(plano);
        await db.SaveChangesAsync();
        return plano.Id;
    }

    private async Task<(string Email, string Senha, int PlanoId, int EstabelecimentoId)> SeedEstabelecimentoAsync()
    {
        const string email = "assinatura-estabelecimento@email.com";
        const string senha = "Senha123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<GLOWAPI.Application.Interfaces.Services.IPasswordHasher>();

        db.Assinaturas.RemoveRange(db.Assinaturas);
        db.ProfissionalEstabelecimentos.RemoveRange(db.ProfissionalEstabelecimentos);
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

        await CampanhaPromocionalTestHelper.DesabilitarPromocaoAsync(db);
        await CategoriaEstabelecimentoTestHelper.GarantirCatalogoAsync(db);
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

    private async Task<(string Email, string Senha, int UsuarioId, int PlanoId)> SeedUsuarioClienteAsync(string email)
    {
        const string senha = "Senha123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<GLOWAPI.Application.Interfaces.Services.IPasswordHasher>();

        db.Usuarios.RemoveRange(db.Usuarios.Where(usuario => usuario.Email == email));

        var usuario = new Usuario
        {
            Nome = "Cliente Ativo",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.Cliente,
            Ativo = true
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return (email, senha, usuario.Id, 0);
    }

    private async Task<(string Email, string Senha, int UsuarioId, int PlanoId)> SeedUsuarioEPlanoAsync(string email)
    {
        const string senha = "Senha123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<GLOWAPI.Application.Interfaces.Services.IPasswordHasher>();

        db.Assinaturas.RemoveRange(db.Assinaturas);
        db.ProfissionalEstabelecimentos.RemoveRange(db.ProfissionalEstabelecimentos);
        db.EstabelecimentoUsuarios.RemoveRange(db.EstabelecimentoUsuarios);
        db.Estabelecimentos.RemoveRange(db.Estabelecimentos);
        db.Planos.RemoveRange(db.Planos);
        db.Usuarios.RemoveRange(db.Usuarios.Where(usuario => usuario.Email == email));

        var usuario = new Usuario
        {
            Nome = "Dono Onboarding",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.DonoEstabelecimento,
            Ativo = true,
            WhatsAppConfirmadoEm = DateTime.UtcNow
        };

        var plano = new Plano
        {
            Nome = $"Plano {Guid.NewGuid()}",
            Descricao = "Plano para onboarding",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            LimiteProfissionais = 5,
            LimiteServicos = 20,
            LimiteAgendamentos = 200,
            Ativo = true
        };

        await CampanhaPromocionalTestHelper.DesabilitarPromocaoAsync(db);
        await CategoriaEstabelecimentoTestHelper.GarantirCatalogoAsync(db);
        db.Usuarios.Add(usuario);
        db.Planos.Add(plano);
        await db.SaveChangesAsync();

        return (email, senha, usuario.Id, plano.Id);
    }

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();
        // Sessão autenticada via cookie HttpOnly guc_access (HandleCookies no client).
    }

    private static object PagamentoValido() => new
    {
        paymentMethodId = "pix"
    };
}
