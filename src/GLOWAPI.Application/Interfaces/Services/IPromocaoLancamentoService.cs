using GLOWAPI.Application.DTOs.Assinaturas;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IPromocaoLancamentoService
{
    Task<PromocaoLancamentoStatusDto> ObterStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> TentarReservarVagaAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<bool> EstabelecimentoJaUsouPromocaoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
}
