using System.Text.Json;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionDestinoHelper
{
    /// <summary>
    /// Evolution API v1.7.x so aceita telefone no sendText; @lid retorna 400 (exists:false).
    /// </summary>
    public static string ResolverDestinoOutbound(string telefoneCadastrado, string? remoteJidConversa)
    {
        _ = remoteJidConversa;
        return TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado);
    }

    public static string? CriarPayloadOutbound(string telefoneCadastrado, string? remoteJidConversa)
    {
        if (!EvolutionWebhookParser.EhRemoteJidLid(remoteJidConversa))
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            telefoneFallback = TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado),
            remoteJidConversa
        });
    }

    public static string? ExtrairTelefoneFallbackDoPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("telefoneFallback", out var telefone)
                && telefone.ValueKind == JsonValueKind.String)
            {
                return TelefoneHelper.NormalizarParaWhatsApp(telefone.GetString() ?? string.Empty);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
