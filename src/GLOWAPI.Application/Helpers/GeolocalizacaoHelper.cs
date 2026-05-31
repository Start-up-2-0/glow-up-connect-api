using System.Globalization;
using System.Text;

namespace GLOWAPI.Application.Helpers;

public static class GeolocalizacaoHelper
{
    public static string NormalizarTextoLocalizacao(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var normalized = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var caractere in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static double CalcularDistanciaKm(
        decimal latitudeOrigem,
        decimal longitudeOrigem,
        decimal latitudeDestino,
        decimal longitudeDestino)
    {
        const double raioTerraKm = 6371d;

        var latOrigemRad = GrausParaRadianos((double)latitudeOrigem);
        var latDestinoRad = GrausParaRadianos((double)latitudeDestino);
        var deltaLat = GrausParaRadianos((double)(latitudeDestino - latitudeOrigem));
        var deltaLng = GrausParaRadianos((double)(longitudeDestino - longitudeOrigem));

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(latOrigemRad) * Math.Cos(latDestinoRad)
            * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return raioTerraKm * c;
    }

    private static double GrausParaRadianos(double graus) => graus * Math.PI / 180d;
}
