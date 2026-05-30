using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IServicoRepository : IRepository<Servico>
{
    Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
