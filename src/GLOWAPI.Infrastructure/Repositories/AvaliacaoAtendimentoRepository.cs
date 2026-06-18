using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AvaliacaoAtendimentoRepository
    : Repository<AvaliacaoAtendimento>, IAvaliacaoAtendimentoRepository
{
    public AvaliacaoAtendimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AvaliacaoAtendimento?> ObterPorAgendamentoIdAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(avaliacao => avaliacao.AgendamentoId == agendamentoId, cancellationToken);

    public async Task<IReadOnlyDictionary<int, AvaliacaoAtendimento>> ObterPorAgendamentoIdsAsync(
        IReadOnlyCollection<int> agendamentoIds,
        CancellationToken cancellationToken = default)
    {
        if (agendamentoIds.Count == 0)
        {
            return new Dictionary<int, AvaliacaoAtendimento>();
        }

        var itens = await DbSet
            .Where(avaliacao => agendamentoIds.Contains(avaliacao.AgendamentoId))
            .ToListAsync(cancellationToken);

        return itens.ToDictionary(avaliacao => avaliacao.AgendamentoId);
    }

    public Task<AvaliacaoAtendimento?> ObterPorAgendamentoIdComDetalhesAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(avaliacao => avaliacao.Estabelecimento)
            .Include(avaliacao => avaliacao.Profissional)
            .FirstOrDefaultAsync(avaliacao => avaliacao.AgendamentoId == agendamentoId, cancellationToken);

    public async Task<IReadOnlyList<AvaliacaoAtendimento>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(avaliacao => avaliacao.UsuarioCliente)
            .Include(avaliacao => avaliacao.Agendamento)
            .Include(avaliacao => avaliacao.Profissional)
            .Where(avaliacao =>
                avaliacao.EstabelecimentoId == estabelecimentoId
                && avaliacao.AvaliadoEm >= avaliadoDesde)
            .OrderByDescending(avaliacao => avaliacao.AvaliadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

    public Task<int> ContarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(
            avaliacao =>
                avaliacao.EstabelecimentoId == estabelecimentoId
                && avaliacao.AvaliadoEm >= avaliadoDesde,
            cancellationToken);

    public async Task<IReadOnlyList<AvaliacaoAtendimento>> ListarPorProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(avaliacao => avaliacao.UsuarioCliente)
            .Include(avaliacao => avaliacao.Agendamento)
            .Include(avaliacao => avaliacao.Profissional)
            .Where(avaliacao =>
                avaliacao.ProfissionalId == profissionalId
                && avaliacao.AvaliadoEm >= avaliadoDesde)
            .OrderByDescending(avaliacao => avaliacao.AvaliadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

    public Task<int> ContarPorProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(
            avaliacao =>
                avaliacao.ProfissionalId == profissionalId
                && avaliacao.AvaliadoEm >= avaliadoDesde,
            cancellationToken);

    public async Task<IReadOnlyList<byte>> ListarNotasEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(avaliacao =>
                avaliacao.EstabelecimentoId == estabelecimentoId
                && avaliacao.AvaliadoEm >= avaliadoDesde)
            .Select(avaliacao => avaliacao.NotaEstabelecimento)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<byte>> ListarNotasProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(avaliacao =>
                avaliacao.ProfissionalId == profissionalId
                && avaliacao.AvaliadoEm >= avaliadoDesde)
            .Select(avaliacao => avaliacao.NotaProfissional)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> ListarEstabelecimentosComAvaliacoesAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Select(avaliacao => avaliacao.EstabelecimentoId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> ListarProfissionaisComAvaliacoesAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Select(avaliacao => avaliacao.ProfissionalId)
            .Distinct()
            .ToListAsync(cancellationToken);
}
