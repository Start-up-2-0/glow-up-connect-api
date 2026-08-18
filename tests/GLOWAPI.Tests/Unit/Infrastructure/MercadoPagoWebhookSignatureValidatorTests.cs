using System.Security.Cryptography;
using System.Text;
using GLOWAPI.Application.Helpers;
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
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveUsarDataIdDaQuery_QuandoDiferenteDoBody()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"173530401375"},"id":106379580211,"action":"payment.created"}""";
        var requestId = "req-live";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var dataIdQuery = "173530401375";
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest(dataIdQuery, requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, dataIdQuery, payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveOmitirRequestIdDoManifest_QuandoHeaderEstiverAusente()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"}}""";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", null, ts);
        Assert.Equal($"id:12345;ts:{ts};", manifest);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, null, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveAceitarQualquerHashV1DoHeader()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"}}""";
        var requestId = "req-rotacao";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifesto = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var v1Valido = ComputeHmacHex(manifesto, Secret);
        var signature = $"ts={ts},v1=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa,v1={v1Valido}";

        var valido = validator.Validar(signature, requestId, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveAceitarTimestampEmMilissegundos()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"}}""";
        var requestId = "req-ms";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveUsarDataIdDoBody_QuandoQueryEstiverVazia()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"},"type":"payment"}""";
        var requestId = "req-body";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, null, payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveRejeitarAssinaturaInvalida()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"},"type":"payment"}""";

        var valido = validator.Validar("ts=1,v1=deadbeef", "req", "12345", payload, out var motivo);

        Assert.False(valido);
        Assert.Equal("assinatura_nao_confere", motivo);
    }

    [Fact]
    public void Validar_DeveAceitarTimestampAntigo_ParaPermitirRetryDoMercadoPago()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"}}""";
        var requestId = "req-retry";
        var ts = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds().ToString();
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void Validar_DeveAceitarSecretComAspas()
    {
        var validator = new MercadoPagoWebhookSignatureValidator(
            Options.Create(new MercadoPagoOptions { WebhookSecret = $"\"{Secret}\"" }));
        var payload = """{"data":{"id":"12345"}}""";
        var requestId = "req-aspas";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var manifest = MercadoPagoWebhookSignatureValidator.MontarManifest("12345", requestId, ts);
        var signature = $"ts={ts},v1={ComputeHmacHex(manifest, Secret)}";

        var valido = validator.Validar(signature, requestId, "12345", payload, out var motivo);

        Assert.True(valido);
        Assert.Null(motivo);
    }

    [Fact]
    public void EhSemAssinatura_DeveDetectarFeedDoMercadoPago()
    {
        Assert.True(MercadoPagoWebhookIpn.EhSemAssinatura(
            null,
            "MercadoPago Feed v2.0 merchant_order",
            temIdentificadorQuery: false));
        Assert.False(MercadoPagoWebhookIpn.EhSemAssinatura(
            "ts=1,v1=abc",
            "MercadoPago Feed v2.0 merchant_order",
            temIdentificadorQuery: true));
    }

    [Fact]
    public void PodeProcessarViaConsultaGateway_DeveAceitarFeedComAssinaturaInvalida()
    {
        Assert.True(MercadoPagoWebhookIpn.PodeProcessarViaConsultaGateway(
            "MercadoPago Feed v2.0 merchant_order",
            temIdentificadorQuery: false));
        Assert.True(MercadoPagoWebhookIpn.PodeProcessarViaConsultaGateway(
            "MercadoPago WebHook v1.0 payment",
            temIdentificadorQuery: true));
        Assert.False(MercadoPagoWebhookIpn.PodeProcessarViaConsultaGateway(
            "Mozilla/5.0",
            temIdentificadorQuery: false));
    }

    [Fact]
    public void ExtrairTimestampAssinatura_DeveRetornarTsSemExporV1()
    {
        Assert.Equal(
            "1700000000",
            MercadoPagoWebhookIpn.ExtrairTimestampAssinatura("ts=1700000000,v1=deadbeef"));
        Assert.Null(MercadoPagoWebhookIpn.ExtrairTimestampAssinatura(null));
    }

    [Fact]
    public void Validar_DeveRejeitarQuandoHashNaoConferir()
    {
        var validator = CriarValidator();
        var payload = """{"data":{"id":"12345"}}""";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var valido = validator.Validar(
            $"ts={ts},v1=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "req",
            "12345",
            payload,
            out var motivo);

        Assert.False(valido);
        Assert.Equal("assinatura_nao_confere", motivo);
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
