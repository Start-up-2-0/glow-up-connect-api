using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ProfissionalEstabelecimentoRepository : Repository<ProfissionalEstabelecimento>, IProfissionalEstabelecimentoRepository
{
    public ProfissionalEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<ProfissionalEstabelecimento?> ObterAtivoPorProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Estabelecimento)
                .ThenInclude(estabelecimento => estabelecimento!.Endereco)
            .FirstOrDefaultAsync(
                vinculo => vinculo.ProfissionalId == profissionalId && vinculo.Ativo,
                cancellationToken);
    }

    public Task<bool> ExisteAtivoAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            vinculo => vinculo.ProfissionalId == profissionalId
                && vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo,
            cancellationToken);
    }
}
