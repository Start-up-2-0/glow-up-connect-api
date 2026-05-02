using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IEstabelecimentoRepository : IRepository<Estabelecimento>
{
    Task<Estabelecimento?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default);
}
