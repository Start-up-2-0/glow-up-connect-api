using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoRepository : Repository<Agendamento>, IAgendamentoRepository
{
    public AgendamentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Agendamento> Itens, int Total)> ListarAgendaGeralAsync(
        AgendaGeralFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var (inicio, fim) = AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(filtro.Inicio, filtro.Fim);
        var (pagina, tamanhoPagina) = AgendaPeriodoConsulta.ResolverPaginacao(filtro.Pagina, filtro.TamanhoPagina);

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

        query = query.Where(agendamento => agendamento.Itens.Any(item =>
            (!filtro.ProfissionalId.HasValue || item.ProfissionalId == filtro.ProfissionalId.Value)
            && item.Inicio >= inicio
            && item.Inicio < fim));

        var total = await query.CountAsync(cancellationToken);
        var skip = (pagina - 1) * tamanhoPagina;

        var itens = await AgendaOrdenacaoConsulta
            .AplicarOrdenacaoAgendamentos(query, filtro.Ordenacao)
            .Skip(skip)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
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
                    .ThenInclude(profissional => profissional!.Usuario)
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
                .ThenInclude(estabelecimento => estabelecimento!.Endereco)
            .Where(agendamento => agendamento.UsuarioClienteId == usuarioClienteId)
            .OrderByDescending(agendamento => agendamento.CreateAd)
            .ToListAsync(cancellationToken);
    }

    public Task<Agendamento?> ObterPorIdEUsuarioClienteAsync(
        int agendamentoId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Servico)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Profissional)
            .Include(agendamento => agendamento.Estabelecimento)
                .ThenInclude(estabelecimento => estabelecimento!.Endereco)
            .Include(agendamento => agendamento.UsuarioCliente)
            .FirstOrDefaultAsync(
                agendamento => agendamento.Id == agendamentoId
                    && agendamento.UsuarioClienteId == usuarioClienteId,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Agendamento> Itens, int Total)> ListarPorUsuarioClienteComFiltroAsync(
        AgendamentoClienteFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Servico)
            .Include(agendamento => agendamento.Itens)
                .ThenInclude(item => item.Profissional)
            .Include(agendamento => agendamento.Estabelecimento)
                .ThenInclude(estabelecimento => estabelecimento!.Endereco)
            .Where(agendamento => agendamento.UsuarioClienteId == filtro.UsuarioClienteId);

        if (filtro.Status.HasValue)
        {
            query = query.Where(agendamento => agendamento.Status == filtro.Status.Value);
        }

        if (filtro.EstabelecimentoId.HasValue)
        {
            query = query.Where(agendamento => agendamento.EstabelecimentoId == filtro.EstabelecimentoId.Value);
        }

        if (filtro.DataInicio.HasValue || filtro.DataFim.HasValue)
        {
            query = query.Where(agendamento => agendamento.Itens.Any(item =>
                (!filtro.DataInicio.HasValue || item.Inicio >= filtro.DataInicio.Value)
                && (!filtro.DataFim.HasValue || item.Inicio < filtro.DataFim.Value)));
        }

        var total = await query.CountAsync(cancellationToken);

        var (pagina, tamanhoPagina) = AgendaPeriodoConsulta.ResolverPaginacao(filtro.Pagina, filtro.TamanhoPagina);
        var skip = (pagina - 1) * tamanhoPagina;

        query = AgendaOrdenacaoConsulta.AplicarOrdenacaoAgendamentos(query, filtro.Ordenacao);

        var itens = await query
            .Skip(skip)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<(decimal TotalValor, int Quantidade)> SomarConcluidosClienteNoPeriodoAsync(
        int usuarioClienteId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(agendamento =>
                agendamento.UsuarioClienteId == usuarioClienteId
                && agendamento.Status == AgendamentoStatus.Concluido
                && agendamento.Itens.Any(item => item.Inicio >= inicio && item.Inicio < fim));

        var quantidade = await query.CountAsync(cancellationToken);
        var totalValor = quantidade == 0
            ? 0m
            : await query.SumAsync(agendamento => agendamento.ValorTotal, cancellationToken);

        return (totalValor, quantidade);
    }

    public Task<int> ContarPorUsuarioClienteAsync(
        int usuarioClienteId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            agendamento => agendamento.UsuarioClienteId == usuarioClienteId,
            cancellationToken);
    }

    public Task<int> ContarPorEstabelecimentoNoPeriodoAsync(
        int estabelecimentoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            agendamento => agendamento.EstabelecimentoId == estabelecimentoId
                && agendamento.Itens.Any(item => item.Inicio >= inicio && item.Inicio <= fim),
            cancellationToken);
    }

    public Task<int> ContarClientesDistintosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .Where(agendamento =>
                agendamento.EstabelecimentoId == estabelecimentoId
                && agendamento.UsuarioClienteId != null)
            .Select(agendamento => agendamento.UsuarioClienteId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agendamento>> ListarConcluidosPorProfissionalNoPeriodoAsync(
        int profissionalId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(a => a.Itens)
            .Where(a => a.Itens.Any(i =>
                i.ProfissionalId == profissionalId
                && i.Status == AgendamentoItemStatus.Concluido
                && i.Inicio >= inicio
                && i.Inicio < fim))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClienteAgendamentoResumo>> ListarClientesResumoPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var agendamentos = await DbSet
            .AsNoTracking()
            .Where(agendamento => agendamento.EstabelecimentoId == estabelecimentoId)
            .Select(agendamento => new
            {
                agendamento.ClienteNome,
                agendamento.ClienteEmail,
                agendamento.ClienteTelefone,
                agendamento.CreateAd
            })
            .ToListAsync(cancellationToken);

        return agendamentos
            .GroupBy(agendamento => new
            {
                Nome = agendamento.ClienteNome ?? "Cliente",
                Email = agendamento.ClienteEmail,
                Telefone = agendamento.ClienteTelefone
            })
            .Select(grupo => new ClienteAgendamentoResumo(
                grupo.Key.Nome,
                grupo.Key.Email,
                grupo.Key.Telefone,
                grupo.Count(),
                grupo.Max(item => (DateTime?)item.CreateAd)))
            .OrderByDescending(cliente => cliente.UltimoAgendamentoEm)
            .ToList();
    }
}
