using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoItemRepository : Repository<AgendamentoItem>, IAgendamentoItemRepository
{
    public AgendamentoItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AgendamentoItem>> ListarAgendaProfissionalAsync(
        AgendaProfissionalFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(item => item.Servico)
            .Include(item => item.Agendamento)
                .ThenInclude(agendamento => agendamento!.UsuarioCliente)
            .Where(item => item.ProfissionalId == filtro.ProfissionalId
                && item.Agendamento != null
                && item.Agendamento.EstabelecimentoId == filtro.EstabelecimentoId);

        if (filtro.Status.HasValue)
        {
            query = query.Where(item => item.Status == filtro.Status.Value);
        }

        if (filtro.Inicio.HasValue)
        {
            query = query.Where(item => item.Inicio >= filtro.Inicio.Value);
        }

        if (filtro.Fim.HasValue)
        {
            query = query.Where(item => item.Inicio < filtro.Fim.Value);
        }

        return await query
            .OrderBy(item => item.Inicio)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<AgendamentoItem?> ObterPorIdComAgendamentoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(item => item.Agendamento)
                .ThenInclude(agendamento => agendamento!.Itens)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> ExisteClienteVinculadoAoProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            item => item.ProfissionalId == profissionalId
                && item.Agendamento != null
                && item.Agendamento.EstabelecimentoId == estabelecimentoId
                && item.Agendamento.UsuarioClienteId == usuarioClienteId,
            cancellationToken);
    }
}
