namespace GLOWAPI.Application.Models.Mensageria;

public sealed record EmailAnexoInline(
    string ContentId,
    string Filename,
    string ContentType,
    byte[] Content);
