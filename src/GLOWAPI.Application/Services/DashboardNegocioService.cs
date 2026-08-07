using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.DTOs.Dashboard;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class DashboardNegocioService : IDashboardNegocioService
{
    private readonly IAgendaNegocioService _agendaNegocioService;
    private readonly IMovimentosFinanceirosService _movimentosFinanceirosService;
    private readonly IClienteNegocioService _clienteNegocioService;
    private readonly IServicoNegocioService _servicoNegocioService;
    private readonly IEquipeNegocioService _equipeNegocioService;
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public DashboardNegocioService(
        IAgendaNegocioService agendaNegocioService,
        IMovimentosFinanceirosService movimentosFinanceirosService,
        IClienteNegocioService clienteNegocioService,
        IServicoNegocioService servicoNegocioService,
        IEquipeNegocioService equipeNegocioService,
        IAvaliacaoResumoService avaliacaoResumoService,
        IAgendamentoRepository agendamentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _agendaNegocioService = agendaNegocioService;
        _movimentosFinanceirosService = movimentosFinanceirosService;
        _clienteNegocioService = clienteNegocioService;
        _servicoNegocioService = servicoNegocioService;
        _equipeNegocioService = equipeNegocioService;
        _avaliacaoResumoService = avaliacaoResumoService;
        _agendamentoRepository = agendamentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<DashboardNegocioResponseDto> ObterAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        // Garante vínculo com o estabelecimento (qualquer papel autenticado do negócio).
        await _autorizacaoNegocioService.ObterContextoAsync(estabelecimentoId, cancellationToken);

        var agora = DateTime.UtcNow;
        var (hojeInicio, hojeFim) = DiaUtc(agora);
        var (ontemInicio, ontemFim) = DiaUtc(agora.AddDays(-1));
        var (mesInicio, _) = MesAtualUtc(agora);
        var (mesAnteriorInicio, mesAnteriorFim) = MesAnteriorUtc(agora);
        var (semanaInicio, _) = SemanaAtualUtc(agora);

        var agendaHojeTask = Tentar(() => _agendaNegocioService.ListarAgendaGeralAsync(
            estabelecimentoId,
            new AgendaGeralFiltroDto
            {
                Inicio = hojeInicio,
                Fim = hojeFim,
                Pagina = 1,
                TamanhoPagina = 50,
                Ordenacao = "atendimento_asc",
            },
            cancellationToken));

        var ultimosTask = Tentar(() => _agendaNegocioService.ListarAgendaGeralAsync(
            estabelecimentoId,
            new AgendaGeralFiltroDto
            {
                Pagina = 1,
                TamanhoPagina = 5,
                Ordenacao = "atendimento_desc",
            },
            cancellationToken));

        var ontemTask = _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
            estabelecimentoId, ontemInicio, ontemFim.AddTicks(-1), cancellationToken);
        var semanaTask = _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
            estabelecimentoId, semanaInicio, hojeFim.AddTicks(-1), cancellationToken);

        var finMesTask = Tentar(() => _movimentosFinanceirosService.ObterDashboardAsync(
            estabelecimentoId, new FinanceiroFiltroDto(mesInicio, agora), cancellationToken));
        var finMesAntTask = Tentar(() => _movimentosFinanceirosService.ObterDashboardAsync(
            estabelecimentoId, new FinanceiroFiltroDto(mesAnteriorInicio, mesAnteriorFim), cancellationToken));
        var finHojeTask = Tentar(() => _movimentosFinanceirosService.ObterDashboardAsync(
            estabelecimentoId, new FinanceiroFiltroDto(hojeInicio, hojeFim), cancellationToken));
        var finSemanaTask = Tentar(() => _movimentosFinanceirosService.ObterDashboardAsync(
            estabelecimentoId, new FinanceiroFiltroDto(semanaInicio, agora), cancellationToken));

        var clientesTask = Tentar(() => _clienteNegocioService.ListarPorEstabelecimentoAsync(
            estabelecimentoId, cancellationToken));
        var servicosTask = Tentar(() => _servicoNegocioService.ListarAsync(
            estabelecimentoId,
            new ServicoFiltroDto { Ativo = true },
            cancellationToken));
        var equipeTask = Tentar(() => _equipeNegocioService.ListarProfissionaisAsync(
            estabelecimentoId, cancellationToken));
        var avaliacaoTask = Tentar(() => _avaliacaoResumoService.ObterResumoEstabelecimentoAsync(
            estabelecimentoId, cancellationToken));

        await Task.WhenAll(
            agendaHojeTask,
            ultimosTask,
            ontemTask,
            semanaTask,
            finMesTask,
            finMesAntTask,
            finHojeTask,
            finSemanaTask,
            clientesTask,
            servicosTask,
            equipeTask,
            avaliacaoTask);

        var agendaHoje = await agendaHojeTask;
        var proximos = agendaHoje?.Itens ?? [];
        var agendamentosHoje = agendaHoje?.Total ?? 0;
        var cancelamentosHoje = proximos.Count(a => a.Status == nameof(AgendamentoStatus.Cancelado));
        var distribuicao = proximos
            .SelectMany(a => a.Itens)
            .GroupBy(i => i.ServicoNome)
            .Select(g => new DashboardNegocioDistribuicaoServicoDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Quantidade)
            .Take(8)
            .ToList();

        var countsHoje = proximos
            .SelectMany(a => a.Itens)
            .GroupBy(i => i.ProfissionalId)
            .ToDictionary(g => g.Key, g => g.Count());

        var profissionais = (await equipeTask)?
            .Where(p => p.Ativo)
            .Take(6)
            .Select(p => new DashboardNegocioProfissionalDto(
                p.Id,
                p.ProfissionalId,
                p.NomePublico,
                p.Ativo,
                p.PodeReceberAgendamento,
                p.NotaMedia,
                countsHoje.GetValueOrDefault(p.ProfissionalId)))
            .ToList()
            ?? [];

        return new DashboardNegocioResponseDto(
            (await finMesTask)?.TotalEntradas ?? 0,
            (await finMesAntTask)?.TotalEntradas ?? 0,
            (await finHojeTask)?.TotalEntradas ?? 0,
            (await finSemanaTask)?.TotalEntradas ?? 0,
            0,
            agendamentosHoje,
            await ontemTask,
            await semanaTask,
            cancelamentosHoje,
            (await clientesTask)?.Count ?? 0,
            (await servicosTask)?.Count ?? 0,
            profissionais,
            (await ultimosTask)?.Itens ?? [],
            proximos,
            await avaliacaoTask,
            [],
            [],
            distribuicao);
    }

    private static async Task<T?> Tentar<T>(Func<Task<T>> acao)
        where T : class
    {
        try
        {
            return await acao();
        }
        catch
        {
            return null;
        }
    }

    private static (DateTime Inicio, DateTime Fim) DiaUtc(DateTime utc)
    {
        var inicio = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, inicio.AddDays(1));
    }

    private static (DateTime Inicio, DateTime Fim) MesAtualUtc(DateTime utc)
    {
        var inicio = new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, utc.AddSeconds(1));
    }

    private static (DateTime Inicio, DateTime Fim) MesAnteriorUtc(DateTime utc)
    {
        var inicioMes = new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicioMes.AddMonths(-1), inicioMes);
    }

    private static (DateTime Inicio, DateTime Fim) SemanaAtualUtc(DateTime utc)
    {
        var day = (int)utc.DayOfWeek;
        var diff = day == 0 ? 6 : day - 1;
        var inicio = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-diff);
        return (inicio, utc.AddSeconds(1));
    }
}
