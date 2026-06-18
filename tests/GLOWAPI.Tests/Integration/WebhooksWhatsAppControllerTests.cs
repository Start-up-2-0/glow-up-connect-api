using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GLOWAPI.Tests.Integration;

public class WebhooksWhatsAppControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WebhooksWhatsAppControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MensagemRecebida_DeveRetornar200_SemAutenticacao()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/whatsapp/evolution/messages-upsert", new
        {
            data = new
            {
                key = new
                {
                    remoteJid = "5511988887777@s.whatsapp.net",
                    fromMe = false
                },
                message = new
                {
                    conversation = "GLOW 482913"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task MensagemEnviada_DeveRetornar200_SemAutenticacao()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/whatsapp/evolution/send-message", new
        {
            data = new
            {
                key = new
                {
                    remoteJid = "5511988887777@s.whatsapp.net",
                    fromMe = true
                },
                message = new
                {
                    conversation = "Ola Maria"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task RotaLegadaEvolution_NaoDeveAceitarWebhookSemAuth()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/whatsapp/evolution", new
        {
            @event = "messages.upsert",
            data = new { }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
