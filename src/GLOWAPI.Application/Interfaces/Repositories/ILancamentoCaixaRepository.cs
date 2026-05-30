using GLOWAPI.Domain.Entities;
using GLOWAPI.Application.Models.Caixa;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ILancamentoCaixaRepository : IRepository<LancamentoCaixa>
{
    Task<IReadOnlyList<LancamentoCaixa>> ListarPorCaixaAsync(
        LancamentoCaixaFiltro filtro,
        CancellationToken cancellationToken = default);
}
