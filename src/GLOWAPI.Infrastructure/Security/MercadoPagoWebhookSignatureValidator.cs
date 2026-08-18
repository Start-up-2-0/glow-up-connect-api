using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class MercadoPagoWebhookSignatureValidator : IMercadoPagoWebhookSignatureValidator
{
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

        var secret = NormalizarSecret(_options.WebhookSecret);
        if (string.IsNullOrWhiteSpace(secret))
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

        var requestId = string.IsNullOrWhiteSpace(requestIdHeader) ? null : requestIdHeader.Trim();
        var candidatosId = ColetarDataIds(dataIdQuery, payload);

        foreach (var dataId in candidatosId)
        {
            foreach (var requestIdCandidato in new[] { requestId, null })
            {
                var manifest = MontarManifest(dataId, requestIdCandidato, timestamp);
                var assinaturaEsperada = ComputeHmacHex(manifest, secret);
                if (assinaturasInformadas.Any(informada => HashesIguais(assinaturaEsperada, informada)))
                {
                    return true;
                }
            }
        }

        motivoFalha = "assinatura_nao_confere";
        return false;
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

    private static string? NormalizarSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        return secret.Trim().Trim('"');
    }

    private static IReadOnlyList<string?> ColetarDataIds(string? dataIdQuery, string payload)
    {
        var ids = new List<string?>();
        AdicionarId(ids, dataIdQuery);
        AdicionarId(ids, ExtrairDataId(payload, preferirData: true));
        AdicionarId(ids, ExtrairDataId(payload, preferirData: false));
        if (ids.Count == 0)
        {
            ids.Add(null);
        }

        return ids;
    }

    private static void AdicionarId(List<string?> ids, string? valor)
    {
        var normalizado = NormalizarDataId(valor);
        if (normalizado is null || ids.Contains(normalizado))
        {
            return;
        }

        ids.Add(normalizado);
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

    private static string? NormalizarDataId(string? dataId)
    {
        if (string.IsNullOrWhiteSpace(dataId))
        {
            return null;
        }

        return dataId.Trim().ToLowerInvariant();
    }

    private static string? ExtrairDataId(string payload, bool preferirData)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (preferirData
                && root.TryGetProperty("data", out var data)
                && data.TryGetProperty("id", out var dataId))
            {
                return ValorId(dataId);
            }

            if (!preferirData && root.TryGetProperty("id", out var id))
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
