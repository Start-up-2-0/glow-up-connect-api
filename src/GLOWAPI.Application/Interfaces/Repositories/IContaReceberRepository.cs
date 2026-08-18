using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IContaReceberRepository : IRepository<ContaReceber>
{
    Task<IReadOnlyList<ContaReceber>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default);

    Task<ContaReceber?> ObterPorIdEEstabelecimentoAsync(
        int contaId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContaReceber>> ListarAbertasVencidasAsync(
        DateTime ateData,
        CancellationToken cancellationToken = default);
}
