using System.Security.Cryptography;
using System.Text;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class MercadoPagoWebhookSignatureValidatorTests
{
    private const string Secret = "mp-webhook-secret-test-key-32chars";

    [Fact]
    public void Validar_DeveAceitarAssinaturaValida()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"},"type":"payment"}""";
        var requestId = "req-001";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifest = $"id:12345;request-id:{requestId};ts:{ts};";
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveRejeitarAssinaturaInvalida()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"},"type":"payment"}""";

        var valido = validator.Validar("ts=1,v1=deadbeef", "req", payload, out var motivo);

        Assert.False(valido);
        Assert.NotNull(motivo);
    }

    private static MercadoPagoWebhookSignatureValidator CriarValidator() =>
        new(Options.Create(new MercadoPagoOptions { WebhookSecret = Secret }));

    private static string ComputeHmacHex(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
