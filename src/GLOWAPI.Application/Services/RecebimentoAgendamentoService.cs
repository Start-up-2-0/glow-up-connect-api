using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class RecebimentoAgendamentoService : IRecebimentoAgendamentoService
{
    private static readonly HashSet<AgendamentoStatus> StatusElegiveisRecebimento =
    [
        AgendamentoStatus.PendentePagamento,
        AgendamentoStatus.Confirmado,
        AgendamentoStatus.PendenteConfirmacao,
        AgendamentoStatus.Remarcado,
        AgendamentoStatus.EmAtendimento,
        AgendamentoStatus.Concluido
    ];

    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IMovimentacaoCaixaService _movimentacaoCaixaService;
    private readonly IComissaoProfissionalRepository _comissaoProfissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IMetaRepository _metaRepository;

    public RecebimentoAgendamentoService(
        IAgendamentoRepository agendamentoRepository,
        IPagamentoRepository pagamentoRepository,
        IMovimentacaoCaixaService movimentacaoCaixaService,
        IComissaoProfissionalRepository comissaoProfissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IMetaRepository metaRepository)
    {
        _agendamentoRepository = agendamentoRepository;
        _pagamentoRepository = pagamentoRepository;
        _movimentacaoCaixaService = movimentacaoCaixaService;
        _comissaoProfissionalRepository = comissaoProfissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _metaRepository = metaRepository;
    }

    public async Task<ReceberAgendamentoResponseDto> ReceberPresencialAsync(
        int estabelecimentoId,
        int agendamentoId,
        ReceberAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var agendamento = await _agendamentoRepository.ObterPorIdEEstabelecimentoComItensAsync(
            agendamentoId,
            estabelecimentoId,
            cancellationToken);

        if (agendamento is null)
        {
            throw new AgendamentoNaoEncontradoException();
        }

        if (!StatusElegiveisRecebimento.Contains(agendamento.Status))
        {
            throw new RecebimentoAgendamentoInvalidoException(
                "O agendamento nao esta em status elegivel para recebimento.");
        }

        if (await _pagamentoRepository.ObterPagoPorAgendamentoAsync(agendamentoId, cancellationToken) is not null)
        {
            throw new AgendamentoJaRecebidoException();
        }

        var valorRecebido = request.Valor ?? agendamento.ValorTotal;
        if (valorRecebido <= 0)
        {
            throw new RecebimentoAgendamentoInvalidoException("O valor do recebimento deve ser maior que zero.");
        }

        var referenciaInterna = $"agendamento-recebimento-{agendamentoId}-{Guid.NewGuid():N}";
        var pagamento = new Pagamento
        {
            AgendamentoId = agendamentoId,
            Gateway = GatewayPagamento.Presencial,
            GatewayPaymentId = referenciaInterna,
            ReferenciaInterna = referenciaInterna,
            MetodoPagamento = request.FormaRecebimento.ToString(),
            Status = PagamentoStatus.Pago,
            Valor = valorRecebido,
            PagoEm = DateTime.UtcNow,
            DataGeracao = DateTime.UtcNow,
            CreateAd = DateTime.UtcNow
        };

        await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        var lancamentoEntrada = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.EntradaAgendamento,
                valorRecebido,
                $"Recebimento presencial - agendamento #{agendamentoId}",
                AgendamentoId: agendamentoId,
                PagamentoId: pagamento.Id),
            cancellationToken);

        var comissaoIds = await RegistrarComissoesAsync(
            estabelecimentoId,
            agendamento,
            valorRecebido,
            cancellationToken);

        if (agendamento.Status == AgendamentoStatus.PendentePagamento)
        {
            agendamento.Status = AgendamentoStatus.Confirmado;
            agendamento.UpdatedAt = DateTime.UtcNow;
            _agendamentoRepository.Atualizar(agendamento);
            await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.AgendamentoRecebidoPresencial,
            nameof(Agendamento),
            agendamentoId,
            new
            {
                agendamentoId,
                pagamentoId = pagamento.Id,
                lancamentoId = lancamentoEntrada.Id,
                valorRecebido,
                forma = request.FormaRecebimento.ToString(),
                comissaoIds
            },
            cancellationToken);

        return new ReceberAgendamentoResponseDto(
            agendamentoId,
            agendamento.Status.ToString(),
            pagamento.Id,
            lancamentoEntrada.Id,
            valorRecebido,
            request.FormaRecebimento.ToString(),
            comissaoIds);
    }

    private async Task<IReadOnlyList<int>> RegistrarComissoesAsync(
        int estabelecimentoId,
        Agendamento agendamento,
        decimal valorRecebido,
        CancellationToken cancellationToken)
    {
        var comissaoIds = new List<int>();
        var referenciaUtc = DateTime.UtcNow;
        var itensPorProfissional = agendamento.Itens
            .GroupBy(item => item.ProfissionalId)
            .ToList();

        foreach (var grupo in itensPorProfissional)
        {
            var profissionalId = grupo.Key;
            var baseCalculo = grupo.Sum(item => item.Valor);
            if (baseCalculo <= 0)
            {
                continue;
            }

            var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
                profissionalId,
                estabelecimentoId,
                cancellationToken);

            if (vinculo is null || !vinculo.Ativo)
            {
                continue;
            }

            var valorComissao = await CalcularMelhorComissaoAsync(
                estabelecimentoId,
                vinculo,
                baseCalculo,
                referenciaUtc,
                cancellationToken);

            if (valorComissao <= 0)
            {
                continue;
            }

            var lancamentoComissao = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
                estabelecimentoId,
                new RegistrarLancamentoCaixaComando(
                    LancamentoCaixaTipo.ComissaoProfissional,
                    valorComissao,
                    $"Comissao profissional - agendamento #{agendamento.Id}",
                    AgendamentoId: agendamento.Id,
                    ProfissionalId: profissionalId),
                cancellationToken);

            comissaoIds.Add(lancamentoComissao.Id);
        }

        return comissaoIds;
    }

    private async Task<decimal> CalcularMelhorComissaoAsync(
        int estabelecimentoId,
        ProfissionalEstabelecimento vinculo,
        decimal baseCalculo,
        DateTime referenciaUtc,
        CancellationToken cancellationToken)
    {
        // 1. Comissão normal por regra (percentual / valor fixo)
        var regra = await _comissaoProfissionalRepository.ObterAtivaPorVinculoAsync(
            vinculo.Id,
            referenciaUtc,
            cancellationToken);

        var valorComissaoRegra = regra is not null
            ? CalcularValorComissao(regra, baseCalculo)
            : 0;

        // 2. Comissão por metas mensais (a melhor meta atingida)
        var valorComissaoMeta = await CalcularMetaComissaoAsync(
            estabelecimentoId,
            vinculo,
            baseCalculo,
            cancellationToken);

        // 3. Usa a maior entre as duas
        return Math.Max(valorComissaoRegra, valorComissaoMeta);
    }

    private async Task<decimal> CalcularMetaComissaoAsync(
        int estabelecimentoId,
        ProfissionalEstabelecimento vinculo,
        decimal baseCalculo,
        CancellationToken cancellationToken)
    {
        var metasAtivas = await _metaRepository.ListarAtivasPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        if (metasAtivas.Count == 0)
        {
            return 0;
        }

        // Período mensal: do início do mês atual até o próximo mês
        var agora = DateTime.UtcNow;
        var inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);

        // Busca todos os atendimentos concluídos do profissional no mês
        var agendamentos = await _agendamentoRepository.ListarConcluidosPorProfissionalNoPeriodoAsync(
            vinculo.ProfissionalId,
            inicioMes,
            fimMes,
            cancellationToken);

        var itensConcluidos = agendamentos
            .SelectMany(a => a.Itens)
            .Where(i => i.ProfissionalId == vinculo.ProfissionalId
                && i.Status == AgendamentoItemStatus.Concluido
                && i.Inicio >= inicioMes
                && i.Inicio < fimMes)
            .ToList();

        var quantidadeRealizada = itensConcluidos.Count;
        var valorRealizado = itensConcluidos.Sum(i => i.Valor);

        decimal maiorComissaoMeta = 0;

        foreach (var meta in metasAtivas)
        {
            var atingida = meta.TipoMeta switch
            {
                TipoMeta.Atendimentos => quantidadeRealizada >= (int)meta.ValorMeta,
                TipoMeta.Faturamento => valorRealizado >= meta.ValorMeta,
                TipoMeta.Mista => quantidadeRealizada >= (int)meta.ValorMeta
                    || valorRealizado >= meta.ValorMeta,
                _ => false
            };

            if (!atingida) continue;

            var comissaoMeta = Math.Round(baseCalculo * meta.PercentualComissao / 100m, 2);
            if (comissaoMeta > maiorComissaoMeta)
            {
                maiorComissaoMeta = comissaoMeta;
            }
        }

        return maiorComissaoMeta;
    }

    private static decimal CalcularValorComissao(ComissaoProfissional regra, decimal baseCalculo)
    {
        return regra.TipoComissao switch
        {
            TipoComissao.Percentual when regra.Percentual.HasValue =>
                Math.Round(baseCalculo * regra.Percentual.Value / 100m, 2),
            TipoComissao.ValorFixo when regra.ValorFixo.HasValue =>
                regra.ValorFixo.Value,
            TipoComissao.Mista when regra.Percentual.HasValue && regra.ValorFixo.HasValue =>
                Math.Round(baseCalculo * regra.Percentual.Value / 100m, 2) + regra.ValorFixo.Value,
            _ => 0
        };
    }
}
