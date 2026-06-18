using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AvaliacaoConviteRepository
    : Repository<AvaliacaoConvite>, IAvaliacaoConviteRepository
{
    public AvaliacaoConviteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AvaliacaoConvite?> ObterPorTokenComAgendamentoAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(convite => convite.Agendamento!)
                .ThenInclude(agendamento => agendamento.Itens)
                    .ThenInclude(item => item.Servico)
            .Include(convite => convite.Agendamento!)
                .ThenInclude(agendamento => agendamento.Itens)
                    .ThenInclude(item => item.Profissional)
            .Include(convite => convite.Agendamento!)
                .ThenInclude(agendamento => agendamento.Estabelecimento)
            .Include(convite => convite.Agendamento!)
                .ThenInclude(agendamento => agendamento.UsuarioCliente)
            .FirstOrDefaultAsync(convite => convite.TokenPublico == tokenPublico, cancellationToken);

    public Task<AvaliacaoConvite?> ObterPorAgendamentoIdAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(convite => convite.AgendamentoId == agendamentoId, cancellationToken);
}
