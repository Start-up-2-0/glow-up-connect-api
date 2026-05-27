using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record PagamentoAssinaturaResponseDto(
    int Id,
    string Status,
    string Gateway,
    string GatewayPaymentId,
    decimal Valor,
    string Moeda,
    string CheckoutUrl,
    string QrCode,
    DateTime? ExpiraEm)
{
    public static PagamentoAssinaturaResponseDto From(
        Pagamento pagamento,
        string checkoutUrl,
        string qrCode) =>
        new(
            pagamento.Id,
            pagamento.Status.ToString(),
            pagamento.Gateway.ToString(),
            pagamento.GatewayPaymentId,
            pagamento.Valor,
            pagamento.Moeda,
            checkoutUrl,
            qrCode,
            pagamento.ExpiraEm);
}
