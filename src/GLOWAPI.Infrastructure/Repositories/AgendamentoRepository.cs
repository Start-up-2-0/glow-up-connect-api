using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoRepository : Repository<Agendamento>, IAgendamentoRepository
{
    public AgendamentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Agendamento>> ListarAgendaGeralAsync(
        AgendaGeralFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(agendamento => agendamento.UsuarioCliente)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Servico)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Profissional)
            .Where(agendamento => agendamento.EstabelecimentoId == filtro.EstabelecimentoId);

        if (filtro.ClienteId.HasValue)
        {
            query = query.Where(agendamento => agendamento.UsuarioClienteId == filtro.ClienteId.Value);
        }

        if (filtro.Status.HasValue)
        {
            query = query.Where(agendamento => agendamento.Status == filtro.Status.Value);
        }

        if (filtro.ProfissionalId.HasValue || filtro.Inicio.HasValue || filtro.Fim.HasValue)
        {
            query = query.Where(agendamento => agendamento.Itens.Any(item =>
                (!filtro.ProfissionalId.HasValue || item.ProfissionalId == filtro.ProfissionalId.Value)
                && (!filtro.Inicio.HasValue || item.Inicio >= filtro.Inicio.Value)
                && (!filtro.Fim.HasValue || item.Inicio < filtro.Fim.Value)));
        }

        return await query
            .OrderBy(agendamento => agendamento.Itens.Min(item => item.Inicio))
            .ThenBy(agendamento => agendamento.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Agendamento?> ObterPorIdEEstabelecimentoComItensAsync(
        int agendamentoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(agendamento => agendamento.Estabelecimento)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Servico)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Profissional)
            .Include(agendamento => agendamento.UsuarioCliente)
            .FirstOrDefaultAsync(
                agendamento => agendamento.Id == agendamentoId
                    && agendamento.EstabelecimentoId == estabelecimentoId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Agendamento>> ListarPorUsuarioClienteAsync(
        int usuarioClienteId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Servico)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Profissional)
            .Include(agendamento => agendamento.Estabelecimento)
            .Where(agendamento => agendamento.UsuarioClienteId == usuarioClienteId)
            .OrderByDescending(agendamento => agendamento.CreateAd)
            .ToListAsync(cancellationToken);
    }
}
