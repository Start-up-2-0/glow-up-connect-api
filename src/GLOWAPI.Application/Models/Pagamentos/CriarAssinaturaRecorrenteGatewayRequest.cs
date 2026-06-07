using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Pagamentos;

public record CriarAssinaturaRecorrenteGatewayRequest(
    GatewayPagamento Gateway,
    string ReferenciaInterna,
    string Descricao,
    decimal Valor,
    string Moeda,
    string PagadorNome,
    string PagadorEmail,
    int? DiasTrial,
    DateTime? PrimeiraCobrancaEm,
    PagamentoTransparenteGatewayRequest? PagamentoTransparente = null,
    IReadOnlyDictionary<string, string>? Metadados = null);
