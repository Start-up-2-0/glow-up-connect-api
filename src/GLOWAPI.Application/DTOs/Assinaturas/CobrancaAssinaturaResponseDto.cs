using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record CobrancaAssinaturaResponseDto(
    int Id,
    decimal Valor,
    string Status,
    string Moeda,
    string? TipoCobranca,
    int NumeroCiclo,
    DateTime? DataVencimento,
    DateTime? DataGeracao,
    DateTime? CicloInicio,
    DateTime? CicloFim,
    string GatewayPaymentId,
    DateTime? PagoEm)
{
    public static CobrancaAssinaturaResponseDto From(Pagamento pagamento) =>
        new(
            pagamento.Id,
            pagamento.Valor,
            pagamento.Status.ToString(),
            pagamento.Moeda,
            pagamento.TipoCobranca?.ToString(),
            pagamento.NumeroCiclo,
            pagamento.DataVencimento,
            pagamento.DataGeracao,
            pagamento.CicloInicio,
            pagamento.CicloFim,
            pagamento.GatewayPaymentId,
            pagamento.PagoEm);
}
