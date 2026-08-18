using GLOWAPI.Application.DTOs.Dashboard;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class DashboardClienteService : IDashboardClienteService
{
    private static readonly HashSet<AgendamentoStatus> StatusProximos =
    [
        AgendamentoStatus.Confirmado,
        AgendamentoStatus.PendenteConfirmacao,
        AgendamentoStatus.Remarcado,
        AgendamentoStatus.EmAtendimento,
        AgendamentoStatus.PendentePagamento
    ];

    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public DashboardClienteService(
        IAgendamentoRepository agendamentoRepository,
        ICurrentUserContext currentUserContext)
    {
        _agendamentoRepository = agendamentoRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<DashboardClienteResponseDto> ObterAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUserContext.UserId.Value;
        var agora = DateTime.UtcNow;
        var (mesInicio, mesFim) = ObterMesAtualUtc(agora);
        var (mesAnteriorInicio, mesAnteriorFim) = ObterMesAnteriorUtc(agora);

        // Sequencial: o DbContext scoped não aceita queries concorrentes (Task.WhenAll).
        var (gastoMes, atendimentosMes) = await _agendamentoRepository.SomarConcluidosClienteNoPeriodoAsync(
            userId, mesInicio, mesFim, cancellationToken);
        var (gastoMesAnterior, _) = await _agendamentoRepository.SomarConcluidosClienteNoPeriodoAsync(
            userId, mesAnteriorInicio, mesAnteriorFim, cancellationToken);
        var totalAgendamentos = await _agendamentoRepository.ContarPorUsuarioClienteAsync(
            userId, cancellationToken);
        var (historicoItens, _) = await _agendamentoRepository.ListarPorUsuarioClienteComFiltroAsync(
            AgendamentoClienteFiltro.Criar(
                userId,
                status: null,
                dataInicio: null,
                dataFim: null,
                estabelecimentoId: null,
                pagina: 1,
                tamanhoPagina: 24,
                ordenacao: "recentes"),
            cancellationToken);
        var (proximosItens, _) = await _agendamentoRepository.ListarPorUsuarioClienteComFiltroAsync(
            AgendamentoClienteFiltro.Criar(
                userId,
                status: null,
                dataInicio: agora,
                dataFim: null,
                estabelecimentoId: null,
                pagina: 1,
                tamanhoPagina: 10,
                ordenacao: "proximos"),
            cancellationToken);

        var proximo = proximosItens
            .Where(a => StatusProximos.Contains(a.Status)
                && AgendamentoHorarioHelper.ObterInicio(a) >= agora)
            .OrderBy(AgendamentoHorarioHelper.ObterInicio)
            .Select(MapearAgendamento)
            .FirstOrDefault();

        var historicoDtos = historicoItens.Select(MapearAgendamento).ToList();
        var relacionamento = MontarRelacionamento(historicoDtos, totalAgendamentos);
        var timeline = MontarTimeline(historicoDtos, limite: 6);

        return new DashboardClienteResponseDto(
            gastoMes,
            gastoMesAnterior,
            atendimentosMes,
            totalAgendamentos,
            proximo,
            timeline,
            relacionamento);
    }

    private static DashboardClienteAgendamentoDto MapearAgendamento(
        Domain.Entities.Agendamento agendamento)
    {
        var estabelecimento = agendamento.Estabelecimento;
        EnderecoResumoDto? endereco = null;
        if (estabelecimento?.Endereco is not null)
        {
            var end = estabelecimento.Endereco;
            endereco = new EnderecoResumoDto(end.Logradouro, end.Bairro, end.Cidade, end.Estado);
        }

        return new DashboardClienteAgendamentoDto(
            agendamento.Id,
            agendamento.Status.ToString(),
            agendamento.ValorTotal,
            AgendamentoHorarioHelper.ObterInicio(agendamento),
            AgendamentoHorarioHelper.ObterFim(agendamento),
            estabelecimento?.PublicGuid ?? Guid.Empty,
            estabelecimento?.Nome ?? string.Empty,
            string.Empty,
            endereco,
            agendamento.Itens
                .OrderBy(i => i.Inicio)
                .Select(i => new DashboardClienteAgendamentoItemDto(
                    i.Id,
                    i.ServicoId,
                    i.Servico?.Nome ?? string.Empty,
                    i.ProfissionalId,
                    i.Profissional?.NomePublico ?? string.Empty,
                    i.Inicio,
                    i.Fim,
                    i.Valor,
                    i.Status.ToString()))
                .ToList());
    }

    private static DashboardClienteRelacionamentoDto MontarRelacionamento(
        IReadOnlyList<DashboardClienteAgendamentoDto> amostra,
        int totalApi)
    {
        var concluidos = amostra
            .Where(a => a.Status is "Concluido" or "Concluído")
            .OrderByDescending(a => a.Inicio)
            .ToList();

        var lojas = new Dictionary<Guid, DashboardClienteLojaFrequenteDto>();
        var profs = new Dictionary<int, DashboardClienteProfissionalFrequenteDto>();
        var servicos = new Dictionary<string, DashboardClienteServicoFrequenteDto>(StringComparer.OrdinalIgnoreCase);
        decimal totalGasto = 0;

        foreach (var ag in concluidos)
        {
            totalGasto += ag.ValorTotal;
            if (!lojas.TryGetValue(ag.EstabelecimentoPublicGuid, out var loja))
            {
                loja = new DashboardClienteLojaFrequenteDto(ag.EstabelecimentoNome, 0, ag.EstabelecimentoPublicGuid);
                lojas[ag.EstabelecimentoPublicGuid] = loja;
            }

            lojas[ag.EstabelecimentoPublicGuid] = loja with { Visitas = loja.Visitas + 1 };

            foreach (var item in ag.Itens)
            {
                if (!profs.TryGetValue(item.ProfissionalId, out var prof))
                {
                    prof = new DashboardClienteProfissionalFrequenteDto(
                        item.ProfissionalNome,
                        0,
                        ag.EstabelecimentoNome);
                    profs[item.ProfissionalId] = prof;
                }

                profs[item.ProfissionalId] = prof with { Visitas = prof.Visitas + 1 };

                var key = item.ServicoNome.Trim().ToLowerInvariant();
                if (!servicos.TryGetValue(key, out var servico))
                {
                    servico = new DashboardClienteServicoFrequenteDto(item.ServicoNome, 0);
                    servicos[key] = servico;
                }

                servicos[key] = servico with { Vezes = servico.Vezes + 1 };
            }
        }

        var ultimo = concluidos.FirstOrDefault();
        DashboardClienteUltimaVisitaDto? ultimaVisita = ultimo is null
            ? null
            : new DashboardClienteUltimaVisitaDto(
                ultimo.Inicio,
                ultimo.Itens.FirstOrDefault()?.ServicoNome ?? "Atendimento",
                ultimo.EstabelecimentoNome);

        int? frequencia = null;
        if (concluidos.Count >= 2)
        {
            var datas = concluidos.Select(c => c.Inicio).OrderBy(d => d).ToList();
            var gaps = new List<double>();
            for (var i = 1; i < datas.Count; i++)
            {
                gaps.Add((datas[i] - datas[i - 1]).TotalDays);
            }

            frequencia = (int)Math.Round(gaps.Average());
        }

        var clienteDesde = concluidos.Count > 0
            ? concluidos.Min(c => c.Inicio)
            : (DateTime?)null;

        return new DashboardClienteRelacionamentoDto(
            lojas.Values.OrderByDescending(l => l.Visitas).FirstOrDefault(),
            profs.Values.OrderByDescending(p => p.Visitas).FirstOrDefault(),
            servicos.Values.OrderByDescending(s => s.Vezes).FirstOrDefault(),
            ultimaVisita,
            clienteDesde,
            totalApi > 0 ? totalApi : concluidos.Count,
            totalGasto,
            frequencia);
    }

    private static IReadOnlyList<DashboardClienteTimelineGrupoDto> MontarTimeline(
        IReadOnlyList<DashboardClienteAgendamentoDto> itens,
        int limite)
    {
        var hoje = DateTime.UtcNow.Date;
        var ontem = hoje.AddDays(-1);
        var grupos = new List<DashboardClienteTimelineGrupoDto>();
        var mapa = new Dictionary<string, DashboardClienteTimelineGrupoDto>();

        foreach (var ag in itens.Take(limite))
        {
            var data = ag.Inicio.ToUniversalTime().Date;
            var key = data.ToString("O");
            var label = data == hoje
                ? "Hoje"
                : data == ontem
                    ? "Ontem"
                    : data.ToString("dd MMM", new System.Globalization.CultureInfo("pt-BR"));

            if (!mapa.TryGetValue(key, out var grupo))
            {
                grupo = new DashboardClienteTimelineGrupoDto(key, label, []);
                mapa[key] = grupo;
                grupos.Add(grupo);
            }

            mapa[key] = grupo with { Items = grupo.Items.Append(ag).ToList() };
            var idx = grupos.FindIndex(g => g.Id == key);
            if (idx >= 0) grupos[idx] = mapa[key];
        }

        return grupos;
    }

    private static (DateTime Inicio, DateTime Fim) ObterMesAtualUtc(DateTime agoraUtc)
    {
        var inicio = new DateTime(agoraUtc.Year, agoraUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, agoraUtc.AddSeconds(1));
    }

    private static (DateTime Inicio, DateTime Fim) ObterMesAnteriorUtc(DateTime agoraUtc)
    {
        var inicioMesAtual = new DateTime(agoraUtc.Year, agoraUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicio = inicioMesAtual.AddMonths(-1);
        return (inicio, inicioMesAtual);
    }
}
