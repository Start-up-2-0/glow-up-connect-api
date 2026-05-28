using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Pagamentos;

public record CriarCobrancaGatewayRequest(
    GatewayPagamento Gateway,
    string ReferenciaInterna,
    string Descricao,
    decimal Valor,
    string Moeda,
    string PagadorNome,
    string PagadorEmail,
    DateTime? ExpiraEm = null,
    IReadOnlyDictionary<string, string>? Metadados = null,
    PagamentoTransparenteGatewayRequest? PagamentoTransparente = null);
