using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class MovimentosFinanceirosService : IMovimentosFinanceirosService
{
    private readonly ICaixaRepository _caixaRepository;
    private readonly ILancamentoCaixaRepository _lancamentoCaixaRepository;
    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly IContaPagarRepository _contaPagarRepository;
    private readonly IFinanceiroNegocioService _financeiroNegocioService;
    private readonly IMovimentacaoCaixaService _movimentacaoCaixaService;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public MovimentosFinanceirosService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        IContaReceberRepository contaReceberRepository,
        IContaPagarRepository contaPagarRepository,
        IFinanceiroNegocioService financeiroNegocioService,
        IMovimentacaoCaixaService movimentacaoCaixaService,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
        _contaReceberRepository = contaReceberRepository;
        _contaPagarRepository = contaPagarRepository;
        _financeiroNegocioService = financeiroNegocioService;
        _movimentacaoCaixaService = movimentacaoCaixaService;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<FinanceiroDashboardResponseDto> ObterDashboardAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await _caixaRepository.ObterPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);

        decimal saldoAtual = 0;
        decimal totalEntradas = 0;
        decimal totalSaidas = 0;
        decimal comissoesPeriodo = 0;

        if (caixa is not null)
        {
            var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
                new LancamentoCaixaFiltro(caixa.Id, filtro.Inicio, filtro.Fim),
                cancellationToken);

            totalEntradas = lancamentos
                .Where(l => LancamentoCaixaClassificador.EhEntrada(l.Tipo))
                .Sum(l => l.Valor);
            totalSaidas = lancamentos
                .Where(l => LancamentoCaixaClassificador.EhSaida(l.Tipo))
                .Sum(l => l.Valor);
            saldoAtual = caixa.SaldoDisponivel;
            comissoesPeriodo = lancamentos
                .Where(l => l.Tipo == LancamentoCaixaTipo.ComissaoProfissional)
                .Sum(l => l.Valor);
        }

        var contasReceber = await _contaReceberRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            null,
            cancellationToken);
        var contasPagar = await _contaPagarRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            null,
            cancellationToken);

        var abertasReceber = contasReceber
            .Where(c => c.Status is ContaFinanceiraStatus.Aberta or ContaFinanceiraStatus.Vencida);
        var abertasPagar = contasPagar
            .Where(c => c.Status is ContaFinanceiraStatus.Aberta or ContaFinanceiraStatus.Vencida);

        var contasEmAberto = abertasReceber.Sum(c => c.Valor) + abertasPagar.Sum(c => c.Valor);
        var qtdAbertas = abertasReceber.Count() + abertasPagar.Count();

        return new FinanceiroDashboardResponseDto(
            saldoAtual,
            totalEntradas,
            totalSaidas,
            totalEntradas - totalSaidas,
            contasEmAberto,
            qtdAbertas,
            comissoesPeriodo,
            filtro.Inicio,
            filtro.Fim);
    }

    public Task<MovimentosFinanceirosPaginadoResponseDto> ListarEntradasAsync(
        int estabelecimentoId,
        MovimentosFinanceirosFiltroDto filtro,
        CancellationToken cancellationToken = default) =>
        ListarAsync(estabelecimentoId, filtro, "entrada", cancellationToken);

    public Task<MovimentosFinanceirosPaginadoResponseDto> ListarSaidasAsync(
        int estabelecimentoId,
        MovimentosFinanceirosFiltroDto filtro,
        CancellationToken cancellationToken = default) =>
        ListarAsync(estabelecimentoId, filtro, "saida", cancellationToken);

    public async Task<MovimentoFinanceiroResponseDto> CriarEntradaAsync(
        int estabelecimentoId,
        CriarMovimentoFinanceiroRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        ValidarCriacao(request);

        if (DeveCriarContaPendente(request))
        {
            var vencimento = request.Vencimento!.Value;
            var conta = await _financeiroNegocioService.CriarContaReceberAsync(
                estabelecimentoId,
                new CriarContaReceberRequestDto(
                    MontarDescricao(request),
                    request.Valor,
                    vencimento,
                    null),
                cancellationToken);

            return MapearContaReceber(conta);
        }

        var forma = string.IsNullOrWhiteSpace(request.FormaPagamento)
            ? string.Empty
            : $" [{request.FormaPagamento.Trim()}]";
        var lancamento = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.AjusteManual,
                request.Valor,
                $"{MontarDescricao(request)}{forma}"),
            cancellationToken);

        return MapearLancamentoEntrada(lancamento);
    }

    public async Task<MovimentoFinanceiroResponseDto> CriarSaidaAsync(
        int estabelecimentoId,
        CriarMovimentoFinanceiroRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        ValidarCriacao(request);

        if (DeveCriarContaPendente(request))
        {
            var categoria = string.IsNullOrWhiteSpace(request.Categoria) ? "Outro" : request.Categoria.Trim();
            var conta = await _financeiroNegocioService.CriarContaPagarAsync(
                estabelecimentoId,
                new CriarContaPagarRequestDto(
                    categoria,
                    categoria,
                    MontarDescricao(request),
                    request.Valor,
                    request.Vencimento!.Value,
                    false),
                cancellationToken);

            return MapearContaPagar(conta);
        }

        var lancamento = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.Saque,
                request.Valor,
                MontarDescricao(request)),
            cancellationToken);

        return MapearLancamentoSaida(lancamento);
    }

    public async Task<MovimentoFinanceiroResponseDto> MarcarEntradaRecebidaAsync(
        int estabelecimentoId,
        string movimentoId,
        BaixarContaRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var (tipo, id) = ParseMovimentoId(movimentoId);
        if (tipo != "conta")
        {
            throw new LancamentoCaixaInvalidoException("Somente entradas pendentes podem ser marcadas como recebidas.");
        }

        var conta = await _financeiroNegocioService.BaixarContaReceberAsync(
            estabelecimentoId,
            id,
            request ?? new BaixarContaRequestDto(null, null),
            cancellationToken);

        return MapearContaReceber(conta);
    }

    public async Task<MovimentoFinanceiroResponseDto> MarcarSaidaPagaAsync(
        int estabelecimentoId,
        string movimentoId,
        BaixarContaRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var (tipo, id) = ParseMovimentoId(movimentoId);
        if (tipo != "conta")
        {
            throw new LancamentoCaixaInvalidoException("Somente saidas pendentes podem ser marcadas como pagas.");
        }

        var conta = await _financeiroNegocioService.BaixarContaPagarAsync(
            estabelecimentoId,
            id,
            request ?? new BaixarContaRequestDto(null, null),
            cancellationToken);

        return MapearContaPagar(conta);
    }

    private async Task<MovimentosFinanceirosPaginadoResponseDto> ListarAsync(
        int estabelecimentoId,
        MovimentosFinanceirosFiltroDto filtro,
        string direcao,
        CancellationToken cancellationToken)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var movimentos = new List<MovimentoFinanceiroResponseDto>();

        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(
                caixa.Id,
                filtro.Inicio,
                filtro.Fim,
                filtro.Q),
            cancellationToken);

        foreach (var l in lancamentos)
        {
            var m = direcao == "entrada" ? MapearLancamentoEntrada(l) : MapearLancamentoSaida(l);
            if (m is not null)
            {
                movimentos.Add(m);
            }
        }

        if (direcao == "entrada")
        {
            var contas = await _contaReceberRepository.ListarPorEstabelecimentoAsync(
                estabelecimentoId,
                null,
                cancellationToken);
            foreach (var c in contas.Where(c =>
                c.Status is ContaFinanceiraStatus.Aberta or ContaFinanceiraStatus.Vencida))
            {
                movimentos.Add(MapearContaReceber(MapearContaReceberEntity(c)));
            }
        }
        else
        {
            var contas = await _contaPagarRepository.ListarPorEstabelecimentoAsync(
                estabelecimentoId,
                null,
                cancellationToken);
            foreach (var c in contas.Where(c =>
                c.Status is ContaFinanceiraStatus.Aberta or ContaFinanceiraStatus.Vencida))
            {
                movimentos.Add(MapearContaPagar(MapearContaPagarEntity(c)));
            }
        }

        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            var statusNorm = NormalizarStatusFiltro(filtro.Status);
            movimentos = movimentos
                .Where(m => m.Status.Equals(statusNorm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filtro.Q))
        {
            var q = filtro.Q.Trim();
            movimentos = movimentos
                .Where(m => m.Descricao.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var ordenados = movimentos
            .OrderByDescending(m => m.Data)
            .ThenByDescending(m => m.Id)
            .ToList();

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanho = Math.Clamp(filtro.TamanhoPagina, 1, 200);
        var total = ordenados.Count;
        var itens = ordenados.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();

        return new MovimentosFinanceirosPaginadoResponseDto(total, pagina, tamanho, itens);
    }

    private static ContaReceberResponseDto MapearContaReceberEntity(ContaReceber c) =>
        new(c.Id, c.EstabelecimentoId, c.AgendamentoId, c.Descricao, c.Valor, c.Vencimento, c.Status.ToString());

    private static ContaPagarResponseDto MapearContaPagarEntity(ContaPagar c) =>
        new(c.Id, c.EstabelecimentoId, c.Fornecedor, c.Categoria, c.Descricao, c.Valor, c.Vencimento, c.Recorrente, c.Status.ToString());

    private static MovimentoFinanceiroResponseDto? MapearLancamentoEntrada(LancamentoCaixa l)
    {
        if (l.Tipo == LancamentoCaixaTipo.Estorno)
        {
            return new MovimentoFinanceiroResponseDto(
                FormatarId("lancamento", l.Id),
                "entrada",
                l.Valor,
                l.Descricao,
                l.CreateAd,
                "estornado",
                "manual",
                null,
                null,
                null,
                l.AgendamentoId,
                new MovimentoFinanceiroAcoesDto(false, false, false));
        }

        if (!LancamentoCaixaClassificador.EhEntrada(l.Tipo))
        {
            return null;
        }

        var origem = l.Tipo switch
        {
            LancamentoCaixaTipo.EntradaAgendamento => "atendimento",
            _ when l.PagamentoId.HasValue => "pagamento_online",
            _ => "manual"
        };

        return new MovimentoFinanceiroResponseDto(
            FormatarId("lancamento", l.Id),
            "entrada",
            l.Valor,
            l.Descricao,
            l.CreateAd,
            "recebido",
            origem,
            ExtrairFormaPagamento(l.Descricao),
            null,
            null,
            l.AgendamentoId,
            new MovimentoFinanceiroAcoesDto(false, false, l.Tipo != LancamentoCaixaTipo.Estorno));
    }

    private static MovimentoFinanceiroResponseDto? MapearLancamentoSaida(LancamentoCaixa l)
    {
        if (l.Tipo == LancamentoCaixaTipo.Estorno)
        {
            return new MovimentoFinanceiroResponseDto(
                FormatarId("lancamento", l.Id),
                "saida",
                l.Valor,
                l.Descricao,
                l.CreateAd,
                "estornado",
                "manual",
                null,
                null,
                null,
                l.AgendamentoId,
                new MovimentoFinanceiroAcoesDto(false, false, false));
        }

        if (!LancamentoCaixaClassificador.EhSaida(l.Tipo) || l.Tipo == LancamentoCaixaTipo.Estorno)
        {
            return null;
        }

        var origem = l.Tipo == LancamentoCaixaTipo.ComissaoProfissional ? "comissao" : "manual";

        return new MovimentoFinanceiroResponseDto(
            FormatarId("lancamento", l.Id),
            "saida",
            l.Valor,
            l.Descricao,
            l.CreateAd,
            "pago",
            origem,
            null,
            ExtrairCategoria(l.Descricao),
            null,
            l.AgendamentoId,
            new MovimentoFinanceiroAcoesDto(false, false, false));
    }

    private static MovimentoFinanceiroResponseDto MapearContaReceber(ContaReceberResponseDto c)
    {
        var status = c.Status switch
        {
            "Vencida" => "vencido",
            "Paga" => "recebido",
            "Cancelada" => "cancelado",
            _ => "pendente"
        };

        return new MovimentoFinanceiroResponseDto(
            FormatarId("conta", c.Id),
            "entrada",
            c.Valor,
            c.Descricao,
            c.Vencimento,
            status,
            c.AgendamentoId.HasValue ? "atendimento" : "conta",
            null,
            null,
            c.Vencimento,
            c.AgendamentoId,
            new MovimentoFinanceiroAcoesDto(
                status is "pendente" or "vencido",
                false,
                false));
    }

    private static MovimentoFinanceiroResponseDto MapearContaPagar(ContaPagarResponseDto c)
    {
        var status = c.Status switch
        {
            "Vencida" => "vencido",
            "Paga" => "pago",
            "Cancelada" => "cancelado",
            _ => "pendente"
        };

        return new MovimentoFinanceiroResponseDto(
            FormatarId("conta", c.Id),
            "saida",
            c.Valor,
            string.IsNullOrWhiteSpace(c.Descricao) ? c.Fornecedor : c.Descricao,
            c.Vencimento,
            status,
            "conta",
            null,
            c.Categoria,
            c.Vencimento,
            null,
            new MovimentoFinanceiroAcoesDto(
                false,
                status is "pendente" or "vencido",
                false));
    }

    private async Task<Caixa> ObterCaixaAsync(int estabelecimentoId, CancellationToken cancellationToken)
    {
        var caixa = await _caixaRepository.ObterPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        if (caixa is null)
        {
            throw new CaixaNegocioNaoEncontradoException();
        }

        return caixa;
    }

    private static void ValidarCriacao(CriarMovimentoFinanceiroRequestDto request)
    {
        if (request.Valor <= 0)
        {
            throw new LancamentoCaixaInvalidoException("Valor invalido.");
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            throw new LancamentoCaixaInvalidoException("Descricao obrigatoria.");
        }
    }

    private static bool DeveCriarContaPendente(CriarMovimentoFinanceiroRequestDto request)
    {
        if (!request.Vencimento.HasValue)
        {
            return false;
        }

        return request.Vencimento.Value.Date > DateTime.UtcNow.Date;
    }

    private static string MontarDescricao(CriarMovimentoFinanceiroRequestDto request) =>
        request.Descricao.Trim();

    private static string FormatarId(string tipo, int id) => $"{tipo}:{id}";

    private static (string Tipo, int Id) ParseMovimentoId(string movimentoId)
    {
        var partes = movimentoId.Split(':', 2);
        if (partes.Length != 2 || !int.TryParse(partes[1], out var id))
        {
            throw new LancamentoCaixaInvalidoException("Identificador de movimento invalido.");
        }

        return (partes[0], id);
    }

    private static string NormalizarStatusFiltro(string status) => status.Trim().ToLowerInvariant() switch
    {
        "aberta" or "aberto" => "pendente",
        "recebida" => "recebido",
        "paga" => "pago",
        _ => status.Trim().ToLowerInvariant()
    };

    private static string? ExtrairFormaPagamento(string descricao)
    {
        var inicio = descricao.LastIndexOf('[');
        var fim = descricao.LastIndexOf(']');
        if (inicio >= 0 && fim > inicio)
        {
            return descricao[(inicio + 1)..fim].Trim();
        }

        return null;
    }

    private static string? ExtrairCategoria(string descricao)
    {
        if (descricao.StartsWith("Baixa conta a pagar", StringComparison.OrdinalIgnoreCase))
        {
            return "Fornecedor";
        }

        return null;
    }
}
