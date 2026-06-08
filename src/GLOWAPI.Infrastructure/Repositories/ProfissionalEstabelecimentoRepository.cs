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

    public Task<ProfissionalEstabelecimento?> ObterAtivoPorUsuarioAsync(
        int usuarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Profissional)
            .FirstOrDefaultAsync(
                vinculo => vinculo.EstabelecimentoId == estabelecimentoId
                    && vinculo.Ativo
                    && vinculo.Profissional != null
                    && vinculo.Profissional.UsuarioId == usuarioId
                    && vinculo.Profissional.Ativo,
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

    public Task<ProfissionalEstabelecimento?> ObterPorProfissionalAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Profissional)
            .FirstOrDefaultAsync(
            vinculo => vinculo.ProfissionalId == profissionalId
                && vinculo.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }

    public Task<bool> ExisteAtivoPorUsuarioAsync(
        int usuarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            vinculo => vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.Profissional != null
                && vinculo.Profissional.UsuarioId == usuarioId
                && vinculo.Profissional.Ativo,
            cancellationToken);
    }

    public Task<int> ContarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            vinculo => vinculo.EstabelecimentoId == estabelecimentoId && vinculo.Ativo,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarAtivosComAgendamentoPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(vinculo => vinculo.Profissional)
            .Where(vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.PodeReceberAgendamento
                && vinculo.Profissional != null
                && vinculo.Profissional.Ativo)
            .OrderBy(vinculo => vinculo.Profissional!.NomePublico)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(vinculo => vinculo.Profissional)
            .Where(vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.Profissional != null
                && vinculo.Profissional.Ativo)
            .OrderBy(vinculo => vinculo.Profissional!.NomePublico)
            .ToListAsync(cancellationToken);
    }
}
