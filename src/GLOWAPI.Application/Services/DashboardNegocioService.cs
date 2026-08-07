using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Dashboard;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class DashboardNegocioService : IDashboardNegocioService
{
    private readonly IAgendaNegocioService _agendaNegocioService;
    private readonly IMovimentosFinanceirosService _movimentosFinanceirosService;
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly ILogger<DashboardNegocioService> _logger;

    public DashboardNegocioService(
        IAgendaNegocioService agendaNegocioService,
        IMovimentosFinanceirosService movimentosFinanceirosService,
        IAvaliacaoResumoService avaliacaoResumoService,
        IAgendamentoRepository agendamentoRepository,
        IServicoRepository servicoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        ILogger<DashboardNegocioService> logger)
    {
        _agendaNegocioService = agendaNegocioService;
        _movimentosFinanceirosService = movimentosFinanceirosService;
        _avaliacaoResumoService = avaliacaoResumoService;
        _agendamentoRepository = agendamentoRepository;
        _servicoRepository = servicoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _logger = logger;
    }

    public async Task<DashboardNegocioResponseDto> ObterAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.ObterContextoAsync(estabelecimentoId, cancellationToken);

        var agora = DateTime.UtcNow;
        var (hojeInicio, hojeFim) = DiaUtc(agora);
        var (ontemInicio, ontemFim) = DiaUtc(agora.AddDays(-1));
        var (mesInicio, _) = MesAtualUtc(agora);
        var (mesAnteriorInicio, mesAnteriorFim) = MesAnteriorUtc(agora);
        var (semanaInicio, _) = SemanaAtualUtc(agora);

        // Sequencial: DbContext scoped não é thread-safe sob Task.WhenAll.
        var agendaHoje = await _agendaNegocioService.ListarAgendaGeralAsync(
            estabelecimentoId,
            new AgendaGeralFiltroDto
            {
                Inicio = hojeInicio,
                Fim = hojeFim,
                Pagina = 1,
                TamanhoPagina = 50,
                Ordenacao = "atendimento_asc",
            },
            cancellationToken);

        var ultimos = await _agendaNegocioService.ListarAgendaGeralAsync(
            estabelecimentoId,
            new AgendaGeralFiltroDto
            {
                Pagina = 1,
                TamanhoPagina = 5,
                Ordenacao = "atendimento_desc",
            },
            cancellationToken);

        var agendamentosOntem = await _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
            estabelecimentoId, ontemInicio, ontemFim.AddTicks(-1), cancellationToken);
        var agendamentosSemana = await _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
            estabelecimentoId, semanaInicio, hojeFim.AddTicks(-1), cancellationToken);
        var clientesAtivos = await _agendamentoRepository.ContarClientesDistintosPorEstabelecimentoAsync(
            estabelecimentoId, cancellationToken);
        var servicosAtivos = await _servicoRepository.ContarAtivosPorEstabelecimentoAsync(
            estabelecimentoId, cancellationToken);

        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId, cancellationToken);

        var avaliacaoResumo = await _avaliacaoResumoService.ObterResumoEstabelecimentoAsync(
            estabelecimentoId, cancellationToken);

        var finMes = await TentarFinanceiro(estabelecimentoId, mesInicio, agora, cancellationToken);
        var finMesAnterior = await TentarFinanceiro(
            estabelecimentoId, mesAnteriorInicio, mesAnteriorFim, cancellationToken);
        var finHoje = await TentarFinanceiro(estabelecimentoId, hojeInicio, hojeFim, cancellationToken);
        var finSemana = await TentarFinanceiro(estabelecimentoId, semanaInicio, agora, cancellationToken);

        var proximos = agendaHoje.Itens;
        var agendamentosHoje = agendaHoje.Total;
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

        var profissionais = vinculos
            .Where(v => v.Ativo && v.Profissional is not null)
            .Take(6)
            .Select(v => new DashboardNegocioProfissionalDto(
                v.Id,
                v.ProfissionalId,
                v.Profissional!.NomePublico,
                v.Ativo,
                v.PodeReceberAgendamento,
                v.Profissional.NotaMedia,
                countsHoje.GetValueOrDefault(v.ProfissionalId)))
            .ToList();

        return new DashboardNegocioResponseDto(
            finMes?.TotalEntradas ?? 0,
            finMesAnterior?.TotalEntradas ?? 0,
            finHoje?.TotalEntradas ?? 0,
            finSemana?.TotalEntradas ?? 0,
            0,
            agendamentosHoje,
            agendamentosOntem,
            agendamentosSemana,
            cancelamentosHoje,
            clientesAtivos,
            servicosAtivos,
            profissionais,
            ultimos.Itens,
            proximos,
            avaliacaoResumo,
            [],
            [],
            distribuicao);
    }

    private async Task<FinanceiroDashboardResponseDto?> TentarFinanceiro(
        int estabelecimentoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _movimentosFinanceirosService.ObterDashboardAsync(
                estabelecimentoId,
                new FinanceiroFiltroDto(inicio, fim),
                cancellationToken);
        }
        catch (UsuarioSemPermissaoNegocioException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao obter financeiro do dashboard do estabelecimento {EstabelecimentoId}",
                estabelecimentoId);
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
