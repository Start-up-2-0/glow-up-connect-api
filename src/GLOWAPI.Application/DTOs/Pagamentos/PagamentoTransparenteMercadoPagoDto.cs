namespace GLOWAPI.Application.DTOs.Pagamentos;

public class PagamentoTransparenteMercadoPagoDto
{
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? IssuerId { get; set; }
    public int? Installments { get; set; }
    public string? IdentificationType { get; set; }
    public string? IdentificationNumber { get; set; }
}
