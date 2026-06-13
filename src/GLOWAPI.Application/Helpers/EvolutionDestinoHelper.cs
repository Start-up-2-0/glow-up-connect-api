using System.Text.Json;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionDestinoHelper
{
    public static string ResolverDestinoOutbound(string telefoneCadastrado) =>
        TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado);

    public static IReadOnlyList<string> CriarCandidatosDestinoOutbound(string telefoneCadastrado)
    {
        var telefone = TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado);
        return string.IsNullOrWhiteSpace(telefone)
            ? Array.Empty<string>()
            : new[] { telefone };
    }

    public static IReadOnlyList<string> CriarCandidatosDestinoOutboundDeMensagem(
        string destinatario,
        string? payloadJson)
    {
        if (EvolutionWebhookParser.EhRemoteJidLid(destinatario))
        {
            var telefoneFallback = ExtrairTelefoneFallbackDoPayload(payloadJson);
            return CriarCandidatosDestinoOutbound(telefoneFallback ?? string.Empty);
        }

        return CriarCandidatosDestinoOutbound(destinatario);
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
