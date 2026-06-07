using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAssinaturaRepository : IRepository<Assinatura>
{
    Task<Assinatura?> ObterAtivaPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<Assinatura?> ObterPorIdComPlanoAsync(int assinaturaId, CancellationToken cancellationToken = default);
    Task<Assinatura?> ObterAtualPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteAtivaOuPendentePorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteComCampanhaPorEstabelecimentoAsync(int estabelecimentoId, string codigoCampanha, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assinatura>> ListarParaAlertaFaturaAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assinatura>> ListarParaGeracaoCobrancaAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken = default);
}
