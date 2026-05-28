namespace GLOWAPI.Application.Models.Pagamentos;

public record PagamentoTransparenteGatewayRequest(
    string PaymentMethodId,
    string? Token = null,
    string? IssuerId = null,
    int? Installments = null,
    string? IdentificationType = null,
    string? IdentificationNumber = null);
