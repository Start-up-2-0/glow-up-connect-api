using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoPropostaRemarcacaoRepository
    : Repository<AgendamentoPropostaRemarcacao>, IAgendamentoPropostaRemarcacaoRepository
{
    public AgendamentoPropostaRemarcacaoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AgendamentoPropostaRemarcacao?> ObterPorTokenComAgendamentoAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(proposta => proposta.Agendamento!)
                .ThenInclude(agendamento => agendamento.Itens)
            .Include(proposta => proposta.Agendamento!)
                .ThenInclude(agendamento => agendamento.Estabelecimento)
            .Include(proposta => proposta.Agendamento!)
                .ThenInclude(agendamento => agendamento.UsuarioCliente)
            .FirstOrDefaultAsync(
                proposta => proposta.TokenPublico == tokenPublico,
                cancellationToken);

    public Task<AgendamentoPropostaRemarcacao?> ObterPendentePorAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Where(proposta =>
                proposta.AgendamentoId == agendamentoId
                && proposta.Status == PropostaRemarcacaoStatus.Pendente
                && proposta.ExpiraEm > DateTime.UtcNow)
            .OrderByDescending(proposta => proposta.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AgendamentoPropostaRemarcacao?> ObterPorIdEAgendamentoAsync(
        int propostaId,
        int agendamentoId,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(proposta => proposta.Agendamento!)
                .ThenInclude(agendamento => agendamento.Itens)
            .FirstOrDefaultAsync(
                proposta => proposta.Id == propostaId && proposta.AgendamentoId == agendamentoId,
                cancellationToken);

    public async Task ExpirarPendentesAnterioresAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        var pendentes = await DbSet
            .Where(proposta =>
                proposta.AgendamentoId == agendamentoId
                && proposta.Status == PropostaRemarcacaoStatus.Pendente)
            .ToListAsync(cancellationToken);

        foreach (var proposta in pendentes)
        {
            proposta.Status = PropostaRemarcacaoStatus.Expirada;
            proposta.RespondidoEm = DateTime.UtcNow;
        }
    }
}
