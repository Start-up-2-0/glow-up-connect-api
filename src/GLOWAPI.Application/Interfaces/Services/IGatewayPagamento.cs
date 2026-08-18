using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IGatewayPagamento
{
    GatewayPagamento GatewaySuportado { get; }

    Task<CriarCobrancaGatewayResponse> CriarCobrancaAsync(
        CriarCobrancaGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<ConsultarPagamentoGatewayResponse> ConsultarPagamentoAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListarPagamentosDaOrdemAsync(
        string ordemId,
        CancellationToken cancellationToken = default);

    Task<CriarAssinaturaRecorrenteGatewayResponse> CriarAssinaturaRecorrenteAsync(
        CriarAssinaturaRecorrenteGatewayRequest request,
        CancellationToken cancellationToken = default);
}
