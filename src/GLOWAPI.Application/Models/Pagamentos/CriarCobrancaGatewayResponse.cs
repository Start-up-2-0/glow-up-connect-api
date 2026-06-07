namespace GLOWAPI.Application.Models.Pagamentos;

public record CriarCobrancaGatewayResponse(
    bool Sucesso,
    string GatewayPaymentId,
    string CheckoutUrl,
    string QrCode,
    string RequestPayload,
    string ResponsePayload,
    string? MensagemErro = null,
    string MetodoPagamento = "Checkout",
    GatewayHttpFailureInfo? FailureInfo = null)
{
    public static CriarCobrancaGatewayResponse Falha(
        string requestPayload,
        string responsePayload,
        string mensagemErro,
        GatewayHttpFailureInfo? failureInfo = null) =>
        new(
            Sucesso: false,
            GatewayPaymentId: string.Empty,
            CheckoutUrl: string.Empty,
            QrCode: string.Empty,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            MensagemErro: mensagemErro,
            FailureInfo: failureInfo);
}
