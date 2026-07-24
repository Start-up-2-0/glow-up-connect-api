namespace GLOWAPI.Infrastructure.Security;

public static class RequestProofPathNormalizer
{
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalizedPath = path.ToLowerInvariant().Trim();

        // Remove trailing slash, but keep "/" for root
        if (normalizedPath.Length > 1 && normalizedPath.EndsWith('/'))
        {
            normalizedPath = normalizedPath.TrimEnd('/');
        }
        else if (normalizedPath.Length == 0)
        {
            normalizedPath = "/";
        }

        return normalizedPath;
    }
}