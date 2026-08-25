using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GLOWAPI.Application.Helpers;

public static partial class ConfirmacaoWhatsAppTokenHelper
{
    public const string TipoConta = "account";
    public const string TipoEstabelecimento = "store";
    public const string TipoProfissionalAutonomo = "autonomous_professional";

    public static string Gerar(int id, string tipo, string telefone)
    {
        var numero = TelefoneHelper.NormalizarParaConfirmacaoInbound(telefone);
        if (id <= 0 || !TipoValido(tipo) || string.IsNullOrWhiteSpace(numero)) return string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new Payload(id, tipo, numero))))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TentarDecodificar(string token, out Payload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var base64 = token.Trim().Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            payload = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            return payload is { Id: > 0 } && TipoValido(payload.Type) && !string.IsNullOrWhiteSpace(payload.Phone);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            payload = null;
            return false;
        }
    }

    public static bool MensagemContemTokenConfirmacao(string textoMensagem, string telefoneRemetente)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        return ExtrairTokensCandidatos(textoMensagem).Any(token =>
            TentarDecodificar(token, out var payload)
            && (string.IsNullOrWhiteSpace(telefoneRemetente)
                || TelefoneHelper.SaoEquivalentes(payload!.Phone, telefoneRemetente)));
    }

    public static bool PareceTentativaConfirmacaoPorToken(string textoMensagem)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        return ExtrairTokensCandidatos(textoMensagem).Any(token => TentarDecodificar(token, out _));
    }

    public static IEnumerable<string> ExtrairTokensCandidatos(string textoMensagem)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            yield break;
        }

        var vistos = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in TokenBase64Regex().Matches(textoMensagem))
        {
            var token = match.Value.Trim();
            if (vistos.Add(token))
            {
                yield return token;
            }
        }
    }

    public static bool TokenPareceTelefoneBrasileiro(string tokenBase64)
    {
        return TentarDecodificar(tokenBase64, out _);
    }

    private static bool TipoValido(string? tipo) =>
        tipo is TipoConta or TipoEstabelecimento or TipoProfissionalAutonomo;

    public sealed record Payload(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("phone")] string Phone);

    [GeneratedRegex(@"(?<![A-Za-z0-9_\-])([A-Za-z0-9_\-]{8,})(?![A-Za-z0-9_\-])", RegexOptions.Compiled)]
    private static partial Regex TokenBase64Regex();
}
