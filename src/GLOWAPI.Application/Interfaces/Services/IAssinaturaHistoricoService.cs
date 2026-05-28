using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaHistoricoService
{
    Task RegistrarAssinaturaAsync(
        Assinatura assinatura,
        string evento,
        AssinaturaStatus? statusAnterior,
        AssinaturaStatus statusNovo,
        Pagamento? pagamento = null,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default);

    Task RegistrarPagamentoAsync(
        Pagamento pagamento,
        string evento,
        PagamentoStatus? statusAnterior,
        PagamentoStatus statusNovo,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default);

    Task RegistrarRecorrenciaAsync(
        Assinatura assinatura,
        string evento,
        string status,
        Pagamento? pagamento = null,
        DateTime? cicloInicio = null,
        DateTime? cicloFim = null,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default);
}
