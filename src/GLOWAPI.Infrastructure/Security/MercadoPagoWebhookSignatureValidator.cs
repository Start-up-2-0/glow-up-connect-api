using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class MercadoPagoWebhookSignatureValidator : IMercadoPagoWebhookSignatureValidator
{
    private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);

    private readonly MercadoPagoOptions _options;

    public MercadoPagoWebhookSignatureValidator(IOptions<MercadoPagoOptions> options)
    {
        _options = options.Value;
    }

    public bool Validar(string? signatureHeader, string? requestIdHeader, string payload, out string? motivoFalha)
    {
        motivoFalha = null;

        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            motivoFalha = "webhook_secret_nao_configurado";
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            motivoFalha = "assinatura_ausente";
            return false;
        }

        if (!TryExtrairAssinatura(signatureHeader, out var timestamp, out var assinaturaInformada))
        {
            motivoFalha = "assinatura_invalida";
            return false;
        }

        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tsUnix))
        {
            motivoFalha = "timestamp_invalido";
            return false;
        }

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(tsUnix);
        if (Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalMinutes) > MaxSkew.TotalMinutes)
        {
            motivoFalha = "timestamp_expirado";
            return false;
        }

        var dataId = ExtrairDataId(payload);
        if (string.IsNullOrWhiteSpace(dataId))
        {
            motivoFalha = "data_id_ausente";
            return false;
        }

        var manifest = $"id:{dataId};request-id:{requestIdHeader ?? string.Empty};ts:{timestamp};";
        var assinaturaEsperada = ComputeHmacHex(manifest, _options.WebhookSecret);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(assinaturaEsperada),
                Encoding.UTF8.GetBytes(assinaturaInformada)))
        {
            motivoFalha = "assinatura_nao_confere";
            return false;
        }

        return true;
    }

    private static bool TryExtrairAssinatura(string header, out string timestamp, out string assinatura)
    {
        timestamp = string.Empty;
        assinatura = string.Empty;

        foreach (var parte in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
            {
                continue;
            }

            if (string.Equals(kv[0], "ts", StringComparison.OrdinalIgnoreCase))
            {
                timestamp = kv[1];
            }
            else if (string.Equals(kv[0], "v1", StringComparison.OrdinalIgnoreCase))
            {
                assinatura = kv[1];
            }
        }

        return !string.IsNullOrWhiteSpace(timestamp) && !string.IsNullOrWhiteSpace(assinatura);
    }

    private static string? ExtrairDataId(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId))
            {
                return dataId.ValueKind switch
                {
                    JsonValueKind.String => dataId.GetString(),
                    JsonValueKind.Number => dataId.GetRawText(),
                    _ => null
                };
            }

            if (root.TryGetProperty("id", out var id))
            {
                return id.ValueKind switch
                {
                    JsonValueKind.String => id.GetString(),
                    JsonValueKind.Number => id.GetRawText(),
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string ComputeHmacHex(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
