using System.Text.RegularExpressions;
using GLOWAPI.Application.Options;

namespace GLOWAPI.Application.Services;

public static class MercadoPagoPayerEmailResolver
{
    private static readonly Regex TestUserPattern = new(
        @"^TESTUSER(?<id>\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Resolver(string? emailUsuario, MercadoPagoOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PayerEmailOverride))
        {
            return NormalizarEmailTeste(options.PayerEmailOverride);
        }

        return emailUsuario?.Trim() ?? string.Empty;
    }

    public static string NormalizarEmailTeste(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return trimmed;
        }

        var match = TestUserPattern.Match(trimmed);
        if (match.Success)
        {
            return $"TESTUSER{match.Groups["id"].Value}@testuser.com";
        }

        if (trimmed.StartsWith("test_user_", StringComparison.OrdinalIgnoreCase))
        {
            return $"{trimmed}@testuser.com";
        }

        return trimmed;
    }
}
