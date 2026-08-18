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

    public bool Validar(
        string? signatureHeader,
        string? requestIdHeader,
        string? dataIdQuery,
        string payload,
        out string? motivoFalha)
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

        if (!TryExtrairAssinatura(signatureHeader, out var timestamp, out var assinaturasInformadas))
        {
            motivoFalha = "assinatura_invalida";
            return false;
        }

        if (!TryObterHorarioEvento(timestamp, out var eventTime))
        {
            motivoFalha = "timestamp_invalido";
            return false;
        }

        if (Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalMinutes) > MaxSkew.TotalMinutes)
        {
            motivoFalha = "timestamp_expirado";
            return false;
        }

        var dataId = NormalizarDataId(dataIdQuery) ?? NormalizarDataId(ExtrairDataId(payload));
        var requestId = string.IsNullOrWhiteSpace(requestIdHeader) ? null : requestIdHeader.Trim();
        var manifest = MontarManifest(dataId, requestId, timestamp);
        var assinaturaEsperada = ComputeHmacHex(manifest, _options.WebhookSecret);

        if (!assinaturasInformadas.Any(informada => HashesIguais(assinaturaEsperada, informada)))
        {
            motivoFalha = "assinatura_nao_confere";
            return false;
        }

        return true;
    }

    internal static string MontarManifest(string? dataId, string? requestId, string timestamp)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(dataId))
        {
            partes.Add($"id:{dataId}");
        }

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            partes.Add($"request-id:{requestId}");
        }

        partes.Add($"ts:{timestamp}");
        return string.Join(";", partes) + ";";
    }

    private static bool TryExtrairAssinatura(
        string header,
        out string timestamp,
        out IReadOnlyList<string> assinaturas)
    {
        timestamp = string.Empty;
        var hashes = new List<string>();

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
            else if (string.Equals(kv[0], "v1", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(kv[1]))
            {
                hashes.Add(kv[1]);
            }
        }

        assinaturas = hashes;
        return !string.IsNullOrWhiteSpace(timestamp) && hashes.Count > 0;
    }

    private static bool TryObterHorarioEvento(string timestamp, out DateTimeOffset eventTime)
    {
        eventTime = default;
        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tsUnix))
        {
            return false;
        }

        try
        {
            eventTime = timestamp.Length >= 13
                ? DateTimeOffset.FromUnixTimeMilliseconds(tsUnix)
                : DateTimeOffset.FromUnixTimeSeconds(tsUnix);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string? NormalizarDataId(string? dataId)
    {
        if (string.IsNullOrWhiteSpace(dataId))
        {
            return null;
        }

        return dataId.Trim().ToLowerInvariant();
    }

    private static string? ExtrairDataId(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId))
            {
                return ValorId(dataId);
            }

            if (root.TryGetProperty("id", out var id))
            {
                return ValorId(id);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? ValorId(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };

    private static bool HashesIguais(string esperado, string informado)
    {
        var informadoNormalizado = informado.Trim().ToLowerInvariant();
        if (esperado.Length != informadoNormalizado.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperado),
            Encoding.UTF8.GetBytes(informadoNormalizado));
    }

    private static string ComputeHmacHex(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
