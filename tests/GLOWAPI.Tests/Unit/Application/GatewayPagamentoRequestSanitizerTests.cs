using GLOWAPI.Application.Services;

namespace GLOWAPI.Tests.Unit.Application;

public class GatewayPagamentoRequestSanitizerTests
{
    [Fact]
    public void Sanitizar_DeveMascararCardTokenId()
    {
        var sanitizado = GatewayPagamentoRequestSanitizer.Sanitizar(
            """{"payer_email":"test@test.com","card_token_id":"abc123","status":"authorized"}""");

        Assert.NotNull(sanitizado);
        var json = System.Text.Json.JsonSerializer.Serialize(sanitizado);
        Assert.Contains("\"card_token_id\":\"***\"", json);
        Assert.DoesNotContain("abc123", json);
    }
}
