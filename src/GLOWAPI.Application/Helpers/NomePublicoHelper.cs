namespace GLOWAPI.Application.Helpers;

/// <summary>
/// Reduz exposição de PII em superfícies públicas (ex.: listagens de avaliações).
/// </summary>
public static class NomePublicoHelper
{
    public static string MascararNomeCliente(string? nomeCompleto)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            return "Cliente";
        }

        var partes = nomeCompleto
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partes.Length == 0)
        {
            return "Cliente";
        }

        var primeiro = CapitalizarPrimeiraLetra(partes[0]);
        if (partes.Length == 1)
        {
            return primeiro;
        }

        var inicialSobrenome = char.ToUpperInvariant(partes[^1][0]);
        return $"{primeiro} {inicialSobrenome}.";
    }

    private static string CapitalizarPrimeiraLetra(string valor)
    {
        if (valor.Length == 1)
        {
            return valor.ToUpperInvariant();
        }

        return char.ToUpperInvariant(valor[0]) + valor[1..].ToLowerInvariant();
    }
}
