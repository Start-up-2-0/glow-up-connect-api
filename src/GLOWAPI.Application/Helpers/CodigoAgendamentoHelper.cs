using System.Security.Cryptography;
using System.Text;

namespace GLOWAPI.Application.Helpers;

/// <summary>
/// Código pessoal permanente do usuário para autenticação no link público de agendamento.
/// Formato: XXXX-XXXX-XXXX (letras, números e símbolos).
/// </summary>
public static class CodigoAgendamentoHelper
{
    public const string ScopeAgendamentoPublico = "agendamento-publico";

    /// <summary>Alfabeto sem caracteres ambíguos (0/O, 1/I/L).</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789@#$%";

    private const int SegmentLength = 4;
    private const int SegmentCount = 3;

    public static string Gerar()
    {
        var chars = new char[SegmentLength * SegmentCount + (SegmentCount - 1)];
        var offset = 0;

        for (var segment = 0; segment < SegmentCount; segment++)
        {
            if (segment > 0)
            {
                chars[offset++] = '-';
            }

            for (var i = 0; i < SegmentLength; i++)
            {
                chars[offset++] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
        }

        return new string(chars);
    }

    public static string Normalizar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(codigo.Length);
        foreach (var c in codigo.Trim().ToUpperInvariant())
        {
            if (c == '-' || c == ' ')
            {
                continue;
            }

            sb.Append(c);
        }

        var compact = sb.ToString();
        if (compact.Length != SegmentLength * SegmentCount)
        {
            return codigo.Trim().ToUpperInvariant();
        }

        return string.Join('-', Enumerable.Range(0, SegmentCount)
            .Select(i => compact.Substring(i * SegmentLength, SegmentLength)));
    }

    public static bool EhEscopoAgendamentoPublico(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return false;
        }

        return metadataJson.Contains($"\"scope\":\"{ScopeAgendamentoPublico}\"", StringComparison.Ordinal)
            || metadataJson.Contains($"\"scope\": \"{ScopeAgendamentoPublico}\"", StringComparison.Ordinal);
    }
}
