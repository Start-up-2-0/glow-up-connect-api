namespace GLOWAPI.Application.Helpers;

public static class EmailDestinoHelper
{
    public static IReadOnlyList<string> Deduplicar(IEnumerable<string> emails)
    {
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultado = new List<string>();

        foreach (var email in emails)
        {
            var normalizado = email.Trim();
            if (string.IsNullOrWhiteSpace(normalizado))
            {
                continue;
            }

            if (vistos.Add(normalizado))
            {
                resultado.Add(normalizado);
            }
        }

        return resultado;
    }
}
