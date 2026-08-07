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
        return await ListarProjecaoLeveAsync(
            vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.PodeReceberAgendamento
                && vinculo.Profissional != null
                && vinculo.Profissional.Ativo,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await ListarProjecaoLeveAsync(
            vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.Profissional != null
                && vinculo.Profissional.Ativo,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarAtivosParaVitrinePorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await ListarProjecaoLeveAsync(
            vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.Ativo
                && vinculo.SomenteExibicao
                && vinculo.Profissional != null
                && vinculo.Profissional.Ativo,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarVitrinePorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await ListarProjecaoLeveAsync(
            vinculo =>
                vinculo.EstabelecimentoId == estabelecimentoId
                && vinculo.SomenteExibicao
                && vinculo.Profissional != null,
            cancellationToken);
    }

    /// <summary>
    /// Projeta vínculo + profissional sem a coluna Logo (longtext/base64).
    /// </summary>
    private async Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarProjecaoLeveAsync(
        System.Linq.Expressions.Expression<Func<ProfissionalEstabelecimento, bool>> predicado,
        CancellationToken cancellationToken)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicado)
            .OrderBy(vinculo => vinculo.Profissional!.NomePublico)
            .Select(vinculo => new ProfissionalEstabelecimento
            {
                Id = vinculo.Id,
                ProfissionalId = vinculo.ProfissionalId,
                EstabelecimentoId = vinculo.EstabelecimentoId,
                Ativo = vinculo.Ativo,
                DataEntrada = vinculo.DataEntrada,
                DataSaida = vinculo.DataSaida,
                PodeReceberAgendamento = vinculo.PodeReceberAgendamento,
                SomenteExibicao = vinculo.SomenteExibicao,
                CreateAd = vinculo.CreateAd,
                UpdatedAt = vinculo.UpdatedAt,
                Profissional = vinculo.Profissional == null
                    ? null
                    : new Profissional
                    {
                        Id = vinculo.Profissional.Id,
                        PublicGuid = vinculo.Profissional.PublicGuid,
                        UsuarioId = vinculo.Profissional.UsuarioId,
                        NomePublico = vinculo.Profissional.NomePublico,
                        Biografia = vinculo.Profissional.Biografia,
                        Logo = string.Empty,
                        Telefone = vinculo.Profissional.Telefone,
                        Email = vinculo.Profissional.Email,
                        TipoProfissional = vinculo.Profissional.TipoProfissional,
                        Ativo = vinculo.Profissional.Ativo,
                        NotaMedia = vinculo.Profissional.NotaMedia,
                        TotalAvaliacoes = vinculo.Profissional.TotalAvaliacoes,
                        CreateAd = vinculo.Profissional.CreateAd,
                        UpdatedAt = vinculo.Profissional.UpdatedAt,
                    },
            })
            .ToListAsync(cancellationToken);
    }
}
