namespace GLOWAPI.Application.Models.Pagamentos;

public record CriarAssinaturaRecorrenteGatewayResponse(
    bool Sucesso,
    string GatewaySubscriptionId,
    string? GatewayCustomerId,
    string RequestPayload,
    string ResponsePayload,
    string? MensagemErro = null)
{
    public static CriarAssinaturaRecorrenteGatewayResponse Falha(
        string requestPayload,
        string responsePayload,
        string mensagemErro) =>
        new(
            Sucesso: false,
            GatewaySubscriptionId: string.Empty,
            GatewayCustomerId: null,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            MensagemErro: mensagemErro);
}
