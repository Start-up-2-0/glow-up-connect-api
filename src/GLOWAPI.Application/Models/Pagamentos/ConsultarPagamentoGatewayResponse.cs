namespace GLOWAPI.Application.Models.Pagamentos;

public record ConsultarPagamentoGatewayResponse(
    bool Sucesso,
    string GatewayPaymentId,
    string Status,
    string ResponsePayload,
    string? PagadorEmail = null,
    string? ReferenciaExterna = null,
    string? MensagemErro = null)
{
    public static ConsultarPagamentoGatewayResponse Falha(
        string gatewayPaymentId,
        string responsePayload,
        string mensagemErro) =>
        new(
            Sucesso: false,
            GatewayPaymentId: gatewayPaymentId,
            Status: string.Empty,
            ResponsePayload: responsePayload,
            PagadorEmail: null,
            MensagemErro: mensagemErro);
}
