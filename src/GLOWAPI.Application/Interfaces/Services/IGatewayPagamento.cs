using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IGatewayPagamento
{
    GatewayPagamento GatewaySuportado { get; }

    Task<CriarCobrancaGatewayResponse> CriarCobrancaAsync(
        CriarCobrancaGatewayRequest request,
        CancellationToken cancellationToken = default);
}
