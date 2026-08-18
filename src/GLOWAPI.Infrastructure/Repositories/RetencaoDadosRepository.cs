using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class RetencaoDadosRepository : IRetencaoDadosRepository
{
    private const string PayloadVazio = "{}";
    private readonly ApplicationDbContext _context;

    public RetencaoDadosRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> RemoverWebhooksProcessadosAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.WebhookPagamentos
            .Where(webhook => webhook.Processado
                && webhook.ProcessadoEm != null
                && webhook.ProcessadoEm < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> RemoverMensagensNotificacaoLogsAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.MensagensNotificacaoLogs
            .Where(log => log.CriadoEm < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<int> RemoverMensagensNotificacaoTerminaisAntesDeAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        await _context.MensagensNotificacaoLogs
            .Where(log =>
                log.MensagemNotificacao.CriadoEm < cutoffUtc
                && (log.MensagemNotificacao.Status == StatusMensagemNotificacao.Enviado
                    || log.MensagemNotificacao.Status == StatusMensagemNotificacao.Falhou
                    || log.MensagemNotificacao.Status == StatusMensagemNotificacao.Cancelado))
            .ExecuteDeleteAsync(cancellationToken);

        return await _context.MensagensNotificacao
            .Where(mensagem =>
                mensagem.CriadoEm < cutoffUtc
                && (mensagem.Status == StatusMensagemNotificacao.Enviado
                    || mensagem.Status == StatusMensagemNotificacao.Falhou
                    || mensagem.Status == StatusMensagemNotificacao.Cancelado))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> RemoverLogsAutenticacaoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.LogsAutenticacao
            .Where(log => log.CreatedAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> RemoverSessoesAutenticacaoExpiradasAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        _context.SessoesAutenticacao
            .Where(sessao => sessao.ExpiraEm < utcNow)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> AnularPayloadAssinaturasHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.AssinaturasHistorico
            .Where(historico => historico.CreateAd < cutoffUtc && historico.PayloadJson != PayloadVazio)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(historico => historico.PayloadJson, PayloadVazio),
                cancellationToken);

    public Task<int> AnularPayloadPagamentosHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.PagamentosHistorico
            .Where(historico => historico.CreateAd < cutoffUtc && historico.PayloadJson != PayloadVazio)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(historico => historico.PayloadJson, PayloadVazio),
                cancellationToken);

    public Task<int> AnularPayloadAgendamentosHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        _context.AgendamentosHistorico
            .Where(historico => historico.CriadoEm < cutoffUtc && historico.PayloadJson != PayloadVazio)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(historico => historico.PayloadJson, PayloadVazio),
                cancellationToken);
}
