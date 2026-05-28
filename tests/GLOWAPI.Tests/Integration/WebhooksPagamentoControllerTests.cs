using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class WebhooksPagamentoControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WebhooksPagamentoControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Registrar_DeveSalvarWebhook_SemAutenticacao()
    {
        await LimparWebhooksAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/pagamentos", new
        {
            gateway = GatewayPagamento.MercadoPago,
            eventId = "evt-int-1",
            eventType = "payment.approved",
            payload = """{"paymentId":"pay-1"}"""
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        Assert.False(data.GetProperty("duplicado").GetBoolean());
        Assert.False(data.GetProperty("processado").GetBoolean());
        Assert.Equal("evt-int-1", data.GetProperty("eventId").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Contains(db.WebhookPagamentos, webhook =>
            webhook.Gateway == GatewayPagamento.MercadoPago
            && webhook.EventId == "evt-int-1"
            && !webhook.Processado);
    }

    [Fact]
    public async Task Registrar_DeveRetornarDuplicado_QuandoEventoJaFoiRegistrado()
    {
        await LimparWebhooksAsync();
        var client = _factory.CreateClient();
        var payload = new
        {
            gateway = GatewayPagamento.AbacatePay,
            eventId = "evt-duplicado",
            eventType = "billing.paid",
            payload = """{"paymentId":"pay-2"}"""
        };

        var primeiro = await client.PostAsJsonAsync("/api/webhooks/pagamentos", payload);
        var segundo = await client.PostAsJsonAsync("/api/webhooks/pagamentos", payload);

        Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);

        var body = await segundo.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("data").GetProperty("duplicado").GetBoolean());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(db.WebhookPagamentos.Where(webhook =>
            webhook.Gateway == GatewayPagamento.AbacatePay
            && webhook.EventId == "evt-duplicado"));
    }

    [Fact]
    public async Task Registrar_DeveAtivarAssinatura_QuandoPagamentoForAprovado()
    {
        var seed = await SeedPagamentoAssinaturaPendenteAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/pagamentos", new
        {
            gateway = GatewayPagamento.MercadoPago,
            eventId = "evt-aprovado-integracao",
            eventType = "payment.approved",
            payload = $$"""{"gatewayPaymentId":"{{seed.GatewayPaymentId}}"}"""
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var webhook = db.WebhookPagamentos.Single(item => item.EventId == "evt-aprovado-integracao");

        var pagamento = await db.Pagamentos.FindAsync(seed.PagamentoId);
        Assert.NotNull(pagamento);
        Assert.Equal(PagamentoStatus.Pago, pagamento!.Status);
        Assert.True(webhook.Processado);
        Assert.NotNull(pagamento.PagoEm);

        var assinatura = await db.Assinaturas.FindAsync(seed.AssinaturaId);
        Assert.NotNull(assinatura);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura!.Status);
        Assert.Equal(seed.PagamentoId, assinatura.UltimoPagamentoId);
        Assert.NotNull(assinatura.Fim);
    }

    [Fact]
    public async Task RegistrarMercadoPago_DeveAtivarAssinatura_QuandoPagamentoConsultadoForAprovado()
    {
        var seed = await SeedPagamentoAssinaturaPendenteAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/pagamentos/mercado-pago", new
        {
            id = "evt-mp-integracao",
            action = "payment.updated",
            data = new
            {
                id = seed.GatewayPaymentId
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var webhook = db.WebhookPagamentos.Single(item => item.EventId == "evt-mp-integracao");
        Assert.Equal("payment.updated", webhook.EventType);
        Assert.True(webhook.Processado);

        var pagamento = await db.Pagamentos.FindAsync(seed.PagamentoId);
        Assert.NotNull(pagamento);
        Assert.Equal(PagamentoStatus.Pago, pagamento!.Status);

        var assinatura = await db.Assinaturas.FindAsync(seed.AssinaturaId);
        Assert.NotNull(assinatura);
        Assert.Equal(AssinaturaStatus.Ativa, assinatura!.Status);
    }

    private async Task LimparWebhooksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.WebhookPagamentos.RemoveRange(db.WebhookPagamentos);
        await db.SaveChangesAsync();
    }

    private async Task<(int AssinaturaId, int PagamentoId, string GatewayPaymentId)> SeedPagamentoAssinaturaPendenteAsync()
    {
        await LimparWebhooksAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Pagamentos.RemoveRange(db.Pagamentos);
        db.Assinaturas.RemoveRange(db.Assinaturas);
        db.Planos.RemoveRange(db.Planos);

        var plano = new Plano
        {
            Nome = $"Plano Webhook {Guid.NewGuid()}",
            Descricao = "Plano para webhook",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var assinatura = new Assinatura
        {
            Plano = plano,
            Status = AssinaturaStatus.PendentePagamento,
            Inicio = DateTime.UtcNow,
            Gateway = GatewayPagamento.MercadoPago,
            ProfissionalAutonomoId = await CriarProfissionalAutonomoAsync(db)
        };

        var gatewayPaymentId = $"pay-webhook-{Guid.NewGuid():N}";
        var pagamento = new Pagamento
        {
            Assinatura = assinatura,
            Gateway = GatewayPagamento.MercadoPago,
            GatewayPaymentId = gatewayPaymentId,
            MetodoPagamento = "Checkout",
            Status = PagamentoStatus.Pendente,
            Valor = plano.Preco,
            Moeda = "BRL"
        };

        db.Planos.Add(plano);
        db.Assinaturas.Add(assinatura);
        db.Pagamentos.Add(pagamento);
        await db.SaveChangesAsync();

        return (assinatura.Id, pagamento.Id, gatewayPaymentId);
    }

    private static async Task<int> CriarProfissionalAutonomoAsync(ApplicationDbContext db)
    {
        var usuario = new Usuario
        {
            Nome = "Webhook Usuario",
            Email = $"webhook-{Guid.NewGuid():N}@email.com",
            Telefone = "11999999999",
            Senha = "hash",
            Role = UserRole.ProfissionalAutonomo,
            Ativo = true
        };

        var profissional = new Profissional
        {
            Usuario = usuario,
            NomePublico = "Webhook Profissional",
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = true
        };

        db.Usuarios.Add(usuario);
        db.Profissionais.Add(profissional);
        await db.SaveChangesAsync();

        return profissional.Id;
    }
}
