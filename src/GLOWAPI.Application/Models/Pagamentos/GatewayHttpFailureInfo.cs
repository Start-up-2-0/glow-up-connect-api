namespace GLOWAPI.Application.Models.Pagamentos;

public record GatewayHttpFailureInfo(
    int? HttpStatusCode = null,
    string? RequestUri = null,
    IReadOnlyDictionary<string, string>? ResponseHeaders = null);
