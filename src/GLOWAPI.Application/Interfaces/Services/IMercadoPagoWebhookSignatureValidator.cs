namespace GLOWAPI.Application.Interfaces.Services;

public interface IMercadoPagoWebhookSignatureValidator
{
    bool Validar(string? signatureHeader, string? requestIdHeader, string payload, out string? motivoFalha);
}
