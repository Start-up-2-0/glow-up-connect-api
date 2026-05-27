namespace GLOWAPI.Application.Models.Pagamentos;

public record CriarCobrancaGatewayResponse(
    bool Sucesso,
    string GatewayPaymentId,
    string CheckoutUrl,
    string QrCode,
    string RequestPayload,
    string ResponsePayload,
    string? MensagemErro = null)
{
    public static CriarCobrancaGatewayResponse Falha(
        string requestPayload,
        string responsePayload,
        string mensagemErro) =>
        new(
            Sucesso: false,
            GatewayPaymentId: string.Empty,
            CheckoutUrl: string.Empty,
            QrCode: string.Empty,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            MensagemErro: mensagemErro);
}
