using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class SessaoCaixaRepository : Repository<SessaoCaixa>, ISessaoCaixaRepository
{
    public SessaoCaixaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<SessaoCaixa?> ObterSessaoAbertaPorCaixaAsync(
        int caixaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            sessao => sessao.CaixaId == caixaId && sessao.Status == SessaoCaixaStatus.Aberta,
            cancellationToken);
    }

    public Task<SessaoCaixa?> ObterPorIdECaixaAsync(
        int sessaoId,
        int caixaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            sessao => sessao.Id == sessaoId && sessao.CaixaId == caixaId,
            cancellationToken);
    }
}
