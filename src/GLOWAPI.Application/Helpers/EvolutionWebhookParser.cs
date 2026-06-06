using System.Text.Json;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionWebhookParser
{
    public static bool IsMensagemInboundDoUsuario(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data))
        {
            return false;
        }

        if (data.TryGetProperty("key", out var key)
            && key.TryGetProperty("fromMe", out var fromMe)
            && fromMe.ValueKind == JsonValueKind.True)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ExtrairTelefoneRemetente(payload));
    }

    public static string ExtrairTelefoneRemetente(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("key", out var key)
            && key.TryGetProperty("remoteJid", out var remoteJid))
        {
            var jid = remoteJid.GetString() ?? string.Empty;
            var telefone = jid.Split('@')[0];
            return TelefoneHelper.NormalizarParaWhatsApp(telefone);
        }

        if (payload.TryGetProperty("sender", out var sender))
        {
            var valor = sender.GetString() ?? string.Empty;
            return TelefoneHelper.NormalizarParaWhatsApp(valor.Split('@')[0]);
        }

        return string.Empty;
    }

    public static string ExtrairTextoMensagem(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("message", out var message))
        {
            return string.Empty;
        }

        if (message.TryGetProperty("conversation", out var conversation))
        {
            return conversation.GetString()?.Trim() ?? string.Empty;
        }

        if (message.TryGetProperty("extendedTextMessage", out var extended)
            && extended.TryGetProperty("text", out var text))
        {
            return text.GetString()?.Trim() ?? string.Empty;
        }

        if (message.TryGetProperty("buttonsResponseMessage", out var buttonResponse)
            && buttonResponse.TryGetProperty("selectedDisplayText", out var selected))
        {
            return selected.GetString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }
}
