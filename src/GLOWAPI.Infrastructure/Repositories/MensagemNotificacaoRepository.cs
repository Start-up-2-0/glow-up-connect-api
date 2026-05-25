using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class MensagemNotificacaoRepository : Repository<MensagemNotificacao>, IMensagemNotificacaoRepository
{
    public MensagemNotificacaoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<MensagemNotificacao?> ObterPorGuidAsync(Guid guid, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(m => m.Guid == guid, cancellationToken);

    public async Task<IReadOnlyList<MensagemNotificacao>> ReservarLoteAsync(
        int batchSize,
        string instanciaWorker,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        var mensagens = await DbSet
            .FromSqlInterpolated($"""
                SELECT * FROM "MensagensNotificacao" AS m
                WHERE m."Status" IN ('Pendente', 'Reprocessar')
                  AND m."Tentativas" < m."MaximoTentativas"
                  AND (m."AgendadoPara" IS NULL OR m."AgendadoPara" <= {utcNow})
                ORDER BY m."Prioridade" DESC, m."CriadoEm" ASC
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (mensagens.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return Array.Empty<MensagemNotificacao>();
        }

        foreach (var mensagem in mensagens)
        {
            mensagem.ReservarParaProcessamento(instanciaWorker, utcNow);
        }

        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return mensagens;
    }

    public async Task<int> RecuperarTravadasAsync(
        int timeoutMinutos,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var limite = utcNow.AddMinutes(-timeoutMinutos);

        return await DbSet
            .Where(m => m.Status == StatusMensagemNotificacao.Processando
                        && m.ProcessamentoIniciadoEm != null
                        && m.ProcessamentoIniciadoEm < limite)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, StatusMensagemNotificacao.Reprocessar)
                    .SetProperty(m => m.InstanciaWorker, (string?)null)
                    .SetProperty(m => m.ProcessamentoIniciadoEm, (DateTime?)null)
                    .SetProperty(m => m.AtualizadoEm, utcNow),
                cancellationToken);
    }

    public async Task AdicionarLogAsync(MensagemNotificacaoLog log, CancellationToken cancellationToken = default)
    {
        await Context.Set<MensagemNotificacaoLog>().AddAsync(log, cancellationToken);
    }
}
