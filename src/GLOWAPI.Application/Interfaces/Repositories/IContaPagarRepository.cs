using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IContaPagarRepository : IRepository<ContaPagar>
{
    Task<IReadOnlyList<ContaPagar>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default);

    Task<ContaPagar?> ObterPorIdEEstabelecimentoAsync(
        int contaId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
