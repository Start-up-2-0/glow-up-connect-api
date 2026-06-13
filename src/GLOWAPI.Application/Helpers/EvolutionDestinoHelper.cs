using System.Text.Json;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionDestinoHelper
{
    /// <summary>
    /// Evolution API v1.7.x so aceita telefone no sendText; @lid retorna 400 (exists:false).
    /// </summary>
    public static string ResolverDestinoOutbound(string telefoneCadastrado, string? remoteJidConversa)
    {
        var candidatos = CriarCandidatosDestinoOutbound(telefoneCadastrado, remoteJidConversa, remoteJidAlt: null);
        return candidatos.Count > 0 ? candidatos[0] : string.Empty;
    }

    public static IReadOnlyList<string> CriarCandidatosDestinoOutbound(
        string telefoneCadastrado,
        string? remoteJidConversa,
        string? remoteJidAlt)
    {
        var candidatos = new List<string>();

        if (!string.IsNullOrWhiteSpace(remoteJidAlt)
            && !EvolutionWebhookParser.EhRemoteJidLid(remoteJidAlt))
        {
            AdicionarCandidato(candidatos, remoteJidAlt);
        }

        var telefoneEvolution = TelefoneHelper.NormalizarParaEvolutionEnvio(telefoneCadastrado);
        if (!string.IsNullOrWhiteSpace(telefoneEvolution))
        {
            AdicionarCandidato(candidatos, telefoneEvolution);
            AdicionarCandidato(candidatos, $"{telefoneEvolution}@s.whatsapp.net");
        }

        var telefone = TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado);
        if (!string.IsNullOrWhiteSpace(telefone)
            && !string.Equals(telefone, telefoneEvolution, StringComparison.Ordinal))
        {
            AdicionarCandidato(candidatos, telefone);
            AdicionarCandidato(candidatos, $"{telefone}@s.whatsapp.net");
        }

        _ = remoteJidConversa;

        return candidatos;
    }

    public static string? CriarPayloadOutbound(
        string telefoneCadastrado,
        string? remoteJidConversa,
        string? remoteJidAlt = null)
    {
        if (!EvolutionWebhookParser.EhRemoteJidLid(remoteJidConversa))
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            telefoneFallback = TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado),
            remoteJidConversa,
            remoteJidAlt
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

    public static string? ExtrairRemoteJidAltDoPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("remoteJidAlt", out var remoteJidAlt)
                && remoteJidAlt.ValueKind == JsonValueKind.String)
            {
                var jid = remoteJidAlt.GetString();
                return string.IsNullOrWhiteSpace(jid) ? null : jid;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public static IReadOnlyList<string> CriarCandidatosDestinoOutboundDeMensagem(
        string destinatario,
        string? payloadJson)
    {
        if (EvolutionWebhookParser.EhRemoteJidLid(destinatario))
        {
            var telefoneFallback = ExtrairTelefoneFallbackDoPayload(payloadJson);
            var remoteJidConversa = ExtrairRemoteJidConversaDoPayload(payloadJson);
            var remoteJidAlt = ExtrairRemoteJidAltDoPayload(payloadJson);
            return CriarCandidatosDestinoOutbound(telefoneFallback ?? string.Empty, remoteJidConversa, remoteJidAlt);
        }

        return CriarCandidatosDestinoOutbound(destinatario, remoteJidConversa: null, remoteJidAlt: null);
    }

    private static string? ExtrairRemoteJidConversaDoPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("remoteJidConversa", out var remoteJidConversa)
                && remoteJidConversa.ValueKind == JsonValueKind.String)
            {
                var jid = remoteJidConversa.GetString();
                return string.IsNullOrWhiteSpace(jid) ? null : jid;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static void AdicionarCandidato(List<string> candidatos, string destino)
    {
        if (string.IsNullOrWhiteSpace(destino)
            || EvolutionWebhookParser.EhRemoteJidLid(destino))
        {
            return;
        }

        if (destino.Contains('@', StringComparison.Ordinal))
        {
            if (!candidatos.Contains(destino, StringComparer.Ordinal))
            {
                candidatos.Add(destino);
            }

            var prefixo = destino.Split('@')[0];
            var telefone = TelefoneHelper.NormalizarParaWhatsApp(prefixo);
            if (!string.IsNullOrWhiteSpace(telefone) && !candidatos.Contains(telefone, StringComparer.Ordinal))
            {
                candidatos.Add(telefone);
            }

            return;
        }

        var normalizado = TelefoneHelper.NormalizarParaWhatsApp(destino);
        if (!string.IsNullOrWhiteSpace(normalizado) && !candidatos.Contains(normalizado, StringComparer.Ordinal))
        {
            candidatos.Add(normalizado);
        }
    }
}
