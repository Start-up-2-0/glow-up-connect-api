using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAssinaturaRepository : IRepository<Assinatura>
{
    Task<Assinatura?> ObterAtivaPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task<Assinatura?> ObterAtivaPorProfissionalAutonomoAsync(int profissionalId, CancellationToken cancellationToken = default);
}
