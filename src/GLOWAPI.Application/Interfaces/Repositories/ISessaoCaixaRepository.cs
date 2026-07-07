using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ISessaoCaixaRepository : IRepository<SessaoCaixa>
{
    Task<SessaoCaixa?> ObterSessaoAbertaPorCaixaAsync(
        int caixaId,
        CancellationToken cancellationToken = default);

    Task<SessaoCaixa?> ObterPorIdECaixaAsync(
        int sessaoId,
        int caixaId,
        CancellationToken cancellationToken = default);
}
