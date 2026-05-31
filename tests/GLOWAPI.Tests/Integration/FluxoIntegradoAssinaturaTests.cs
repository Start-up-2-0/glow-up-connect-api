using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class FluxoIntegradoAssinaturaTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FluxoIntegradoAssinaturaTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Estabelecimento_DeveContratarPagarELiberarModulos_ComWebhookIdempotente()
    {
        var seed = await SeedUsuarioEPlanoAsync("fluxo-estabelecimento@email.com", UserRole.DonoEstabelecimento);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var iniciarResponse = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.Estabelecimento,
            pagamento = PagamentoValido(),
            estabelecimento = new
            {
                nome = "Studio Fluxo",
                descricao = "Fluxo integrado",
                logo = "https://cdn.test/studio-fluxo.png",
                telefone = "11999999999",
                email = "studio-fluxo@email.com",
                endereco = new
                {
                    cep = "01310100",
                    logradouro = "Rua Fluxo",
                    numero = "100",
                    bairro = "Centro",
                    cidade = "Sao Paulo",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, iniciarResponse.StatusCode);

        var iniciarBody = await iniciarResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var assinaturaData = iniciarBody.GetProperty("data");
        var assinaturaId = assinaturaData.GetProperty("id").GetInt32();
        var estabelecimentoId = assinaturaData.GetProperty("estabelecimentoId").GetInt32();
        var gatewayPaymentId = assinaturaData
            .GetProperty("pagamentoInicial")
            .GetProperty("gatewayPaymentId")
            .GetString();

        Assert.Equal("PendentePagamento", assinaturaData.GetProperty("status").GetString());
        Assert.False(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Agenda));

        var webhookPayload = new
        {
            gateway = GatewayPagamento.MercadoPago,
            eventId = $"evt-fluxo-estabelecimento-{Guid.NewGuid():N}",
            eventType = "payment.approved",
            payload = $$"""{"gatewayPaymentId":"{{gatewayPaymentId}}","email":"{{seed.Email}}"}"""
        };

        var webhookResponse = await client.PostAsJsonAsync("/api/webhooks/pagamentos", webhookPayload);
        var webhookDuplicadoResponse = await client.PostAsJsonAsync("/api/webhooks/pagamentos", webhookPayload);

        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, webhookDuplicadoResponse.StatusCode);

        var webhookBody = await webhookResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var webhookDuplicadoBody = await webhookDuplicadoResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(webhookBody.GetProperty("data").GetProperty("processado").GetBoolean());
        Assert.True(webhookDuplicadoBody.GetProperty("data").GetProperty("duplicado").GetBoolean());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assinatura = await db.Assinaturas.FindAsync(assinaturaId);
        Assert.NotNull(assinatura);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura!.Status);
        Assert.NotNull(assinatura.Fim);

        Assert.Single(db.WebhookPagamentos.Where(webhook => webhook.EventId == webhookPayload.eventId));
        Assert.Single(db.Pagamentos.Where(pagamento =>
            pagamento.AssinaturaId == assinaturaId
            && pagamento.Status == PagamentoStatus.Pago));
        Assert.Contains(db.MensagensNotificacao, mensagem =>
            mensagem.Destinatario == seed.Email
            && mensagem.Assunto == "Pagamento confirmado");

        Assert.True(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Agenda));
        Assert.True(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Caixa));
    }

    [Fact]
    public async Task ProfissionalAutonomo_DeveContratarPagarELiberarModulos()
    {
        var seed = await SeedUsuarioEPlanoAsync("fluxo-autonomo@email.com", UserRole.ProfissionalAutonomo);
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.Email, seed.Senha);

        var iniciarResponse = await client.PostAsJsonAsync("/api/assinaturas", new
        {
            planoId = seed.PlanoId,
            tipoAssinatura = TipoAssinatura.ProfissionalAutonomo,
            pagamento = PagamentoValido(),
            profissionalAutonomo = new
            {
                nomePublico = "Autonomo Fluxo",
                biografia = "Fluxo integrado autonomo",
                logo = "https://cdn.test/autonomo-fluxo.png",
                telefone = "11988888888",
                email = "autonomo-fluxo@email.com",
                endereco = new
                {
                    cep = "13010000",
                    logradouro = "Sala Fluxo",
                    numero = "12",
                    bairro = "Centro",
                    cidade = "Campinas",
                    estado = "SP"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, iniciarResponse.StatusCode);

        var iniciarBody = await iniciarResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var assinaturaData = iniciarBody.GetProperty("data");
        var assinaturaId = assinaturaData.GetProperty("id").GetInt32();
        var estabelecimentoId = assinaturaData.GetProperty("estabelecimentoId").GetInt32();
        var gatewayPaymentId = assinaturaData
            .GetProperty("pagamentoInicial")
            .GetProperty("gatewayPaymentId")
            .GetString();

        Assert.Equal("PendentePagamento", assinaturaData.GetProperty("status").GetString());
        Assert.True(estabelecimentoId > 0);
        Assert.False(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Servicos));

        var webhookResponse = await client.PostAsJsonAsync("/api/webhooks/pagamentos", new
        {
            gateway = GatewayPagamento.MercadoPago,
            eventId = $"evt-fluxo-autonomo-{Guid.NewGuid():N}",
            eventType = "payment.approved",
            payload = $$"""{"gatewayPaymentId":"{{gatewayPaymentId}}","email":"{{seed.Email}}"}"""
        });

        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var webhookBody = await webhookResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(webhookBody.GetProperty("data").GetProperty("processado").GetBoolean());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var assinatura = await db.Assinaturas.FindAsync(assinaturaId);
        Assert.NotNull(assinatura);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura!.Status);
        Assert.Equal(estabelecimentoId, assinatura.EstabelecimentoId);

        var profissional = db.Profissionais.Single(item => item.UsuarioId == seed.UsuarioId);
        Assert.Contains(db.ProfissionalEstabelecimentos, vinculo =>
            vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.ProfissionalId == profissional.Id
            && vinculo.Ativo);

        Assert.Contains(db.MensagensNotificacao, mensagem =>
            mensagem.Destinatario == seed.Email
            && mensagem.Assunto == "Pagamento confirmado");

        Assert.True(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Servicos));
        Assert.True(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Agenda));
        Assert.False(await PossuiModuloEstabelecimentoAsync(estabelecimentoId, ModuloAssinatura.Profissionais));
    }

    private async Task<(string Email, string Senha, int UsuarioId, int PlanoId)> SeedUsuarioEPlanoAsync(
        string email,
        UserRole role)
    {
        const string senha = "Senha123!";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        db.MensagensNotificacao.RemoveRange(db.MensagensNotificacao);
        db.WebhookPagamentos.RemoveRange(db.WebhookPagamentos);
        db.Pagamentos.RemoveRange(db.Pagamentos);
        db.Assinaturas.RemoveRange(db.Assinaturas);
        db.ProfissionalEstabelecimentos.RemoveRange(db.ProfissionalEstabelecimentos);
        db.EstabelecimentoUsuarios.RemoveRange(db.EstabelecimentoUsuarios);
        db.Estabelecimentos.RemoveRange(db.Estabelecimentos);
        db.Profissionais.RemoveRange(db.Profissionais);
        db.Planos.RemoveRange(db.Planos);
        db.Usuarios.RemoveRange(db.Usuarios.Where(usuario => usuario.Email == email));

        var usuario = new Usuario
        {
            Nome = "Usuario Fluxo",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = role,
            Ativo = true
        };

        var plano = new Plano
        {
            Nome = role == UserRole.DonoEstabelecimento ? "Premium" : "Basic",
            Descricao = "Plano para fluxo integrado",
            Preco = role == UserRole.DonoEstabelecimento ? 199.90m : 0m,
            Periodo = PlanoPeriodo.Mensal,
            LimiteProfissionais = role == UserRole.DonoEstabelecimento ? null : 1,
            LimiteServicos = role == UserRole.DonoEstabelecimento ? null : 10,
            LimiteAgendamentos = role == UserRole.DonoEstabelecimento ? null : 10,
            Ativo = true
        };

        db.Usuarios.Add(usuario);
        db.Planos.Add(plano);
        await db.SaveChangesAsync();

        return (email, senha, usuario.Id, plano.Id);
    }

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = loginBody.GetProperty("data").GetProperty("token").GetString();
        client.DefaultRequestHeaders.Add("x-glow-token", token);
    }

    private async Task<bool> PossuiModuloEstabelecimentoAsync(int estabelecimentoId, ModuloAssinatura modulo)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IModulosAssinaturaService>();
        return await service.PossuiModuloPorEstabelecimentoAsync(estabelecimentoId, modulo);
    }

    private static object PagamentoValido() => new
    {
        paymentMethodId = "pix"
    };
}
