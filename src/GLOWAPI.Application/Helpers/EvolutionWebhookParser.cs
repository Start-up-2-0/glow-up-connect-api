using System.Text.Json;
using GLOWAPI.Application.Models.Mensageria;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionWebhookParser
{
    public static string? ExtrairEvento(JsonElement payload) =>
        payload.TryGetProperty("event", out var evento) ? evento.GetString() : null;

    public static string? ExtrairInstancia(JsonElement payload) =>
        payload.TryGetProperty("instance", out var instancia) ? instancia.GetString() : null;

    public static bool? ExtrairFromMe(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("key", out var key)
            || !key.TryGetProperty("fromMe", out var fromMe))
        {
            return null;
        }

        return fromMe.ValueKind == JsonValueKind.True;
    }

    /// <summary>
    /// Campo <c>sender</c> no root do payload Evolution: numero conectado na instancia,
    /// nao o remetente real da mensagem inbound (fromMe=false).
    /// </summary>
    public static string? ExtrairSenderInstancia(JsonElement payload) =>
        payload.TryGetProperty("sender", out var sender) ? sender.GetString() : null;

    public static string? ExtrairPushName(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("pushName", out var pushName))
        {
            return pushName.GetString();
        }

        return null;
    }

    public static string DescreverMotivoNaoInbound(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out _))
        {
            return "sem_campo_data";
        }

        if (string.IsNullOrWhiteSpace(ExtrairTelefoneRemetente(payload)))
        {
            return "telefone_nao_extraido";
        }

        return "desconhecido";
    }

    public static bool IsMensagemInboundDoUsuario(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out _))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ExtrairTelefoneRemetente(payload));
    }

    public static EvolutionWebhookRemetenteDiagnostico ExtrairDiagnosticoRemetente(JsonElement payload)
    {
        var telefone = ExtrairTelefoneRemetente(payload);
        var remoteJid = ExtrairRemoteJidConversa(payload);
        var ehLid = EhRemoteJidLid(remoteJid);

        var remoteJidAltPresente = false;
        var senderPnKeyPresente = false;
        var senderPnDataPresente = false;
        var participantPresente = false;

        if (payload.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("key", out var key))
            {
                remoteJidAltPresente = key.TryGetProperty("remoteJidAlt", out _);
                senderPnKeyPresente = key.TryGetProperty("senderPn", out _);
                participantPresente = key.TryGetProperty("participant", out _);
            }

            senderPnDataPresente = data.TryGetProperty("senderPn", out _);
            participantPresente = participantPresente || data.TryGetProperty("participant", out _);
        }

        return new EvolutionWebhookRemetenteDiagnostico(
            RemoteJid: remoteJid,
            EhLid: ehLid,
            RemoteJidAltPresente: remoteJidAltPresente,
            SenderPnKeyPresente: senderPnKeyPresente,
            SenderPnDataPresente: senderPnDataPresente,
            ParticipantPresente: participantPresente,
            SenderInstancia: ExtrairSenderInstancia(payload),
            PushName: ExtrairPushName(payload),
            FromMe: ExtrairFromMe(payload),
            TelefoneExtraido: telefone,
            MotivoTelefoneVazio: string.IsNullOrWhiteSpace(telefone)
                ? DescreverMotivoTelefoneVazio(payload, remoteJid, ehLid, ExtrairFromMe(payload))
                : null);
    }

    public static string ExtrairTelefoneRemetente(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("key", out var key))
        {
            return string.Empty;
        }

        var telefone = ResolverTelefoneDeKey(key);
        if (!string.IsNullOrWhiteSpace(telefone))
        {
            return telefone;
        }

        telefone = ResolverTelefoneDePropriedadesJid(data);
        if (!string.IsNullOrWhiteSpace(telefone))
        {
            return telefone;
        }

        if (key.TryGetProperty("fromMe", out var fromMe)
            && fromMe.ValueKind == JsonValueKind.True)
        {
            return NormalizarJid(ExtrairSenderInstancia(payload));
        }

        return string.Empty;
    }

    public static string? ExtrairRemoteJidConversa(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("key", out var key)
            && key.TryGetProperty("remoteJid", out var remoteJid))
        {
            var jid = remoteJid.GetString();
            return string.IsNullOrWhiteSpace(jid) ? null : jid;
        }

        return null;
    }

    public static string? ExtrairRemoteJidAlt(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("key", out var key)
            && key.TryGetProperty("remoteJidAlt", out var remoteJidAlt))
        {
            var jid = remoteJidAlt.GetString();
            return string.IsNullOrWhiteSpace(jid) ? null : jid;
        }

        return null;
    }

    public static bool EhRemoteJidLid(string? remoteJid) =>
        !string.IsNullOrWhiteSpace(remoteJid)
        && remoteJid.Contains("@lid", StringComparison.OrdinalIgnoreCase);

    public static string ExtrairTextoMensagem(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("message", out var message))
        {
            return string.Empty;
        }

        return ExtrairTextoDeMessageObject(message);
    }

    private static string DescreverMotivoTelefoneVazio(
        JsonElement payload,
        string? remoteJid,
        bool ehLid,
        bool? fromMe)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("key", out _))
        {
            return "sem_data_key";
        }

        if (ehLid && fromMe == false)
        {
            return "lid_sem_telefone_no_payload";
        }

        if (ehLid && fromMe == true)
        {
            return "lid_from_me_sem_sender_instancia";
        }

        if (string.IsNullOrWhiteSpace(remoteJid))
        {
            return "remote_jid_ausente";
        }

        return "telefone_nao_normalizado";
    }

    private static string ResolverTelefoneDeKey(JsonElement key)
    {
        if (!key.TryGetProperty("remoteJid", out var remoteJid))
        {
            return ResolverTelefoneDePropriedadesJid(key);
        }

        var jid = remoteJid.GetString() ?? string.Empty;
        if (jid.Contains("@lid", StringComparison.OrdinalIgnoreCase))
        {
            if (key.TryGetProperty("remoteJidAlt", out var remoteJidAlt))
            {
                var telefoneAlt = NormalizarJid(remoteJidAlt.GetString());
                if (!string.IsNullOrWhiteSpace(telefoneAlt))
                {
                    return telefoneAlt;
                }
            }

            if (key.TryGetProperty("senderPn", out var senderPn))
            {
                var telefonePn = NormalizarJid(senderPn.GetString());
                if (!string.IsNullOrWhiteSpace(telefonePn))
                {
                    return telefonePn;
                }
            }

            var telefoneParticipant = ResolverTelefoneDePropriedadesJid(key);
            if (!string.IsNullOrWhiteSpace(telefoneParticipant))
            {
                return telefoneParticipant;
            }

            return string.Empty;
        }

        return NormalizarJid(jid);
    }

    private static string ResolverTelefoneDePropriedadesJid(JsonElement element)
    {
        foreach (var propertyName in new[] { "senderPn", "participant" })
        {
            if (!element.TryGetProperty(propertyName, out var jidProperty))
            {
                continue;
            }

            var telefone = NormalizarJid(jidProperty.GetString());
            if (!string.IsNullOrWhiteSpace(telefone))
            {
                return telefone;
            }
        }

        return string.Empty;
    }

    private static string NormalizarJid(string? jid)
    {
        if (string.IsNullOrWhiteSpace(jid))
        {
            return string.Empty;
        }

        if (jid.Contains("@lid", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var prefixo = jid.Split('@')[0];
        return TelefoneHelper.NormalizarParaWhatsApp(prefixo);
    }

    private static string ExtrairTextoDeMessageObject(JsonElement message)
    {
        if (message.TryGetProperty("conversation", out var conversation))
        {
            var texto = conversation.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("extendedTextMessage", out var extended)
            && extended.TryGetProperty("text", out var text))
        {
            var texto = text.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("buttonsResponseMessage", out var buttonResponse)
            && buttonResponse.TryGetProperty("selectedDisplayText", out var selected))
        {
            var texto = selected.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("ephemeralMessage", out var ephemeral)
            && ephemeral.TryGetProperty("message", out var ephemeralMessage))
        {
            var texto = ExtrairTextoDeMessageObject(ephemeralMessage);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        return string.Empty;
    }
}
