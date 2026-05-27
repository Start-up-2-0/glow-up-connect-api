using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private async Task LimparWebhooksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.WebhookPagamentos.RemoveRange(db.WebhookPagamentos);
        await db.SaveChangesAsync();
    }
}
