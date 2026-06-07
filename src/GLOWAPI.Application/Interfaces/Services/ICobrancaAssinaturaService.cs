using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICobrancaAssinaturaService
{
    Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode)> GerarCobrancaInicialAsync(
        Assinatura assinatura,
        Plano plano,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken = default);

    Task<Pagamento> GerarCobrancaRecorrenteAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default);

    Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode)> GerarCobrancaTrocaPlanoAsync(
        Assinatura assinatura,
        Plano novoPlano,
        GatewayPagamento gateway,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken = default);

    Task ProcessarPagamentoAprovadoAsync(
        Pagamento pagamento,
        string payloadJson,
        CancellationToken cancellationToken = default);

    Task ProcessarPagamentoRecusadoAsync(
        Pagamento pagamento,
        string payloadJson,
        PagamentoStatus? novoStatus = null,
        CancellationToken cancellationToken = default);

    Task<int> MarcarAtrasadasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CobrancaAssinaturaResponseDto>> ListarPorAssinaturaAsync(
        int assinaturaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
