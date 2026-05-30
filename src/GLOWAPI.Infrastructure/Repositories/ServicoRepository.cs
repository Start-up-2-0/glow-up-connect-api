using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ServicoRepository : Repository<Servico>, IServicoRepository
{
    public ServicoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            servico => servico.Id == servicoId
                && servico.EstabelecimentoId == estabelecimentoId
                && servico.Ativo,
            cancellationToken);
    }
}
