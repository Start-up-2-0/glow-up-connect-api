using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoItemRepository : Repository<AgendamentoItem>, IAgendamentoItemRepository
{
    public AgendamentoItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<AgendamentoItem> Itens, int Total)> ListarAgendaProfissionalAsync(
        AgendaProfissionalFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var (inicio, fim) = AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(filtro.Inicio, filtro.Fim);
        var (pagina, tamanhoPagina) = AgendaPeriodoConsulta.ResolverPaginacao(filtro.Pagina, filtro.TamanhoPagina);

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
            query = query.Where(item => item.Agendamento!.Status == filtro.Status.Value);
        }

        query = query.Where(item => item.Inicio >= inicio && item.Inicio < fim);

        var total = await query.CountAsync(cancellationToken);
        var skip = (pagina - 1) * tamanhoPagina;

        var itens = await AgendaOrdenacaoConsulta
            .AplicarOrdenacaoItens(query, filtro.Ordenacao)
            .Skip(skip)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
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

    public async Task<bool> ExisteAgendamentoFuturoImpactadoPorAlteracaoHorarioAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim,
        CancellationToken cancellationToken = default)
    {
        var impactados = await ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
            estabelecimentoId,
            profissionalId,
            diaSemanaAtual,
            horaInicioAtual,
            horaFimAtual,
            novoDiaSemana,
            novaHoraInicio,
            novaHoraFim,
            cancellationToken);

        return impactados.Count > 0;
    }

    public async Task<IReadOnlyList<AgendamentoFuturoImpactadoDto>> ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim,
        CancellationToken cancellationToken = default)
    {
        var itensFuturos = await ListarItensFuturosAtivosAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        return itensFuturos
            .Where(item => ItemImpactadoPorAlteracaoHorario(
                item,
                diaSemanaAtual,
                horaInicioAtual,
                horaFimAtual,
                novoDiaSemana,
                novaHoraInicio,
                novaHoraFim))
            .Select(MapearImpactado)
            .ToList();
    }

    public async Task<IReadOnlyList<AgendamentoFuturoImpactadoDto>> ListarAgendamentosFuturosNoHorarioAtivoAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken = default)
    {
        var itensFuturos = await ListarItensFuturosAtivosAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        return itensFuturos
            .Where(item => ItemDentroDoHorario(item, diaSemana, horaInicio, horaFim))
            .Select(MapearImpactado)
            .ToList();
    }

    public async Task<IReadOnlyList<AgendamentoItem>> ListarOcupacaoAsync(
        int estabelecimentoId,
        int? profissionalId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(item => item.Agendamento)
            .Where(item => item.Inicio < fim
                && item.Fim > inicio
                && item.Status != AgendamentoItemStatus.Cancelado
                && item.Agendamento != null
                && item.Agendamento.EstabelecimentoId == estabelecimentoId
                && item.Agendamento.Status != AgendamentoStatus.Cancelado
                && item.Agendamento.Status != AgendamentoStatus.Expirado
                && item.Agendamento.Status != AgendamentoStatus.Reembolsado
                && item.Agendamento.Status != AgendamentoStatus.NaoCompareceu);

        if (profissionalId.HasValue)
        {
            query = query.Where(item => item.ProfissionalId == profissionalId.Value);
        }

        return await query
            .OrderBy(item => item.Inicio)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteFuturoConfirmadoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;

        return DbSet.AnyAsync(
            item => item.ProfissionalId == profissionalId
                && item.ServicoId == servicoId
                && item.Inicio >= agora
                && (item.Status == AgendamentoItemStatus.Pendente
                    || item.Status == AgendamentoItemStatus.Confirmado
                    || item.Status == AgendamentoItemStatus.EmAtendimento)
                && item.Agendamento != null
                && item.Agendamento.Status != AgendamentoStatus.Cancelado
                && item.Agendamento.Status != AgendamentoStatus.Expirado
                && item.Agendamento.Status != AgendamentoStatus.Reembolsado,
            cancellationToken);
    }

    private async Task<List<AgendamentoItem>> ListarItensFuturosAtivosAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;

        return await DbSet
            .AsNoTracking()
            .Include(item => item.Agendamento)
            .Where(item => item.ProfissionalId == profissionalId
                && item.Inicio >= agora
                && item.Status != AgendamentoItemStatus.Cancelado
                && item.Status != AgendamentoItemStatus.Concluido
                && item.Agendamento != null
                && item.Agendamento.EstabelecimentoId == estabelecimentoId
                && item.Agendamento.Status != AgendamentoStatus.Cancelado
                && item.Agendamento.Status != AgendamentoStatus.Concluido
                && item.Agendamento.Status != AgendamentoStatus.Expirado
                && item.Agendamento.Status != AgendamentoStatus.Reembolsado)
            .ToListAsync(cancellationToken);
    }

    private static bool ItemImpactadoPorAlteracaoHorario(
        AgendamentoItem item,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim)
    {
        return ItemDentroDoHorario(item, diaSemanaAtual, horaInicioAtual, horaFimAtual)
            && !ItemDentroDoHorario(item, novoDiaSemana, novaHoraInicio, novaHoraFim);
    }

    private static bool ItemDentroDoHorario(
        AgendamentoItem item,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim)
    {
        var inicio = TimeOnly.FromDateTime(item.Inicio);
        var fim = TimeOnly.FromDateTime(item.Fim);

        return item.Inicio.DayOfWeek == diaSemana
            && inicio >= horaInicio
            && fim <= horaFim;
    }

    private static AgendamentoFuturoImpactadoDto MapearImpactado(AgendamentoItem item) =>
        new()
        {
            AgendamentoItemId = item.Id,
            AgendamentoId = item.AgendamentoId,
            ProfissionalId = item.ProfissionalId,
            Inicio = item.Inicio,
            Fim = item.Fim
        };
}
