using System.Globalization;
using System.Text;
using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace GLOWAPI.Application.Services;

public class FinanceiroNegocioService : IFinanceiroNegocioService
{
    private readonly ICaixaRepository _caixaRepository;
    private readonly ILancamentoCaixaRepository _lancamentoCaixaRepository;
    private readonly IComissaoProfissionalRepository _comissaoProfissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly IContaPagarRepository _contaPagarRepository;
    private readonly IConciliacaoItemRepository _conciliacaoItemRepository;
    private readonly IMovimentacaoCaixaService _movimentacaoCaixaService;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public FinanceiroNegocioService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        IComissaoProfissionalRepository comissaoProfissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IContaReceberRepository contaReceberRepository,
        IContaPagarRepository contaPagarRepository,
        IConciliacaoItemRepository conciliacaoItemRepository,
        IMovimentacaoCaixaService movimentacaoCaixaService,
        IAgendamentoRepository agendamentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
        _comissaoProfissionalRepository = comissaoProfissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _contaReceberRepository = contaReceberRepository;
        _contaPagarRepository = contaPagarRepository;
        _conciliacaoItemRepository = conciliacaoItemRepository;
        _movimentacaoCaixaService = movimentacaoCaixaService;
        _agendamentoRepository = agendamentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<FinanceiroResumoResponseDto> ObterResumoAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(caixa.Id, filtro.Inicio, filtro.Fim),
            cancellationToken);

        var entradas = lancamentos
            .Where(l => LancamentoCaixaClassificador.EhEntrada(l.Tipo))
            .Sum(l => l.Valor);
        var saidas = lancamentos
            .Where(l => LancamentoCaixaClassificador.EhSaida(l.Tipo))
            .Sum(l => l.Valor);

        return new FinanceiroResumoResponseDto(
            caixa.SaldoTotal,
            caixa.SaldoDisponivel,
            caixa.SaldoRetido,
            entradas,
            saidas,
            lancamentos.Count,
            filtro.Inicio,
            filtro.Fim);
    }

    public async Task<IReadOnlyList<LancamentoCaixaResponseDto>> ListarRelatorioAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(caixa.Id, filtro.Inicio, filtro.Fim),
            cancellationToken);

        return lancamentos.Select(LancamentoCaixaResponseDto.From).ToList();
    }

    public async Task<IReadOnlyList<ComissaoProfissionalResponseDto>> ListarComissoesAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        return await ListarComissoesInternoAsync(estabelecimentoId, cancellationToken);
    }

    public async Task<ComissaoProfissionalResponseDto> CriarComissaoAsync(
        int estabelecimentoId,
        CriarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        ValidarComissaoRequest(request.TipoComissao, request.Percentual, request.ValorFixo);

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorIdAsync(
            request.ProfissionalEstabelecimentoId,
            cancellationToken);

        if (vinculo is null || vinculo.EstabelecimentoId != estabelecimentoId)
        {
            throw new ComissaoProfissionalInvalidaException("Vinculo profissional invalido.");
        }

        var comissao = new ComissaoProfissional
        {
            ProfissionalEstabelecimentoId = request.ProfissionalEstabelecimentoId,
            TipoComissao = request.TipoComissao,
            Percentual = request.Percentual,
            ValorFixo = request.ValorFixo,
            Ativo = true,
            InicioVigencia = request.InicioVigencia,
            FimVigencia = request.FimVigencia,
            CreateAd = DateTime.UtcNow
        };

        await _comissaoProfissionalRepository.AdicionarAsync(comissao, cancellationToken);
        await _comissaoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ComissaoRegraCriada,
            nameof(ComissaoProfissional),
            comissao.Id,
            request,
            cancellationToken);

        return ComissaoProfissionalResponseDto.From(comissao, vinculo);
    }

    public async Task<ComissaoProfissionalResponseDto> AtualizarComissaoAsync(
        int estabelecimentoId,
        int comissaoId,
        AtualizarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        ValidarComissaoRequest(request.TipoComissao, request.Percentual, request.ValorFixo);

        var comissao = await _comissaoProfissionalRepository.ObterPorIdComTrackingAsync(comissaoId, cancellationToken);
        if (comissao is null)
        {
            throw new ComissaoProfissionalInvalidaException("Regra de comissao nao encontrada.");
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorIdAsync(
            comissao.ProfissionalEstabelecimentoId,
            cancellationToken);

        if (vinculo is null || vinculo.EstabelecimentoId != estabelecimentoId)
        {
            throw new ComissaoProfissionalInvalidaException("Regra de comissao fora do estabelecimento.");
        }

        comissao.TipoComissao = request.TipoComissao;
        comissao.Percentual = request.Percentual;
        comissao.ValorFixo = request.ValorFixo;
        comissao.InicioVigencia = request.InicioVigencia;
        comissao.FimVigencia = request.FimVigencia;
        comissao.Ativo = request.Ativo;
        comissao.UpdatedAt = DateTime.UtcNow;

        _comissaoProfissionalRepository.Atualizar(comissao);
        await _comissaoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ComissaoRegraAlterada,
            nameof(ComissaoProfissional),
            comissao.Id,
            request,
            cancellationToken);

        return ComissaoProfissionalResponseDto.From(comissao, vinculo);
    }

    public async Task DesativarComissaoAsync(
        int estabelecimentoId,
        int comissaoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var comissao = await _comissaoProfissionalRepository.ObterPorIdComTrackingAsync(comissaoId, cancellationToken);
        if (comissao is null)
        {
            throw new ComissaoProfissionalInvalidaException("Regra de comissao nao encontrada.");
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorIdAsync(
            comissao.ProfissionalEstabelecimentoId,
            cancellationToken);

        if (vinculo is null || vinculo.EstabelecimentoId != estabelecimentoId)
        {
            throw new ComissaoProfissionalInvalidaException("Regra de comissao fora do estabelecimento.");
        }

        comissao.Ativo = false;
        comissao.FimVigencia = DateTime.UtcNow;
        comissao.UpdatedAt = DateTime.UtcNow;

        _comissaoProfissionalRepository.Atualizar(comissao);
        await _comissaoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ComissaoRegraDesativada,
            nameof(ComissaoProfissional),
            comissao.Id,
            new { comissao.Id },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ComissaoProfissionalExtratoDto>> ListarMinhasComissoesAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ComissaoVisualizarPropria,
            cancellationToken);

        if (!_currentUserContext.UserId.HasValue)
        {
            return Array.Empty<ComissaoProfissionalExtratoDto>();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorUsuarioAsync(
            _currentUserContext.UserId.Value,
            estabelecimentoId,
            cancellationToken);

        if (vinculo is null)
        {
            return Array.Empty<ComissaoProfissionalExtratoDto>();
        }

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var lancamentos = await _lancamentoCaixaRepository.ListarPorProfissionalAsync(
            caixa.Id,
            vinculo.ProfissionalId,
            filtro.Inicio,
            filtro.Fim,
            cancellationToken);

        return lancamentos
            .Select(l => new ComissaoProfissionalExtratoDto(
                l.Id,
                l.AgendamentoId,
                l.Valor,
                l.Descricao,
                l.CreateAd))
            .ToList();
    }

    public async Task<RelatorioAnaliticoResponseDto> ObterRelatorioAnaliticoAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(caixa.Id, filtro.Inicio, filtro.Fim),
            cancellationToken);

        var entradasAgendamento = lancamentos
            .Where(l => l.Tipo == LancamentoCaixaTipo.EntradaAgendamento)
            .ToList();

        var faturamento = entradasAgendamento.Sum(l => l.Valor);
        var atendimentos = entradasAgendamento.Count;
        var ticketMedio = atendimentos > 0 ? Math.Round(faturamento / atendimentos, 2) : 0;

        var profissionais = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var nomesProfissionais = profissionais.ToDictionary(
            p => p.ProfissionalId,
            p => p.Profissional?.NomePublico ?? $"Profissional #{p.ProfissionalId}");

        var porProfissional = lancamentos
            .Where(l => l.Tipo == LancamentoCaixaTipo.ComissaoProfissional && l.ProfissionalId.HasValue)
            .GroupBy(l => l.ProfissionalId!.Value)
            .Select(g => new RelatorioPorProfissionalDto(
                g.Key,
                nomesProfissionais.TryGetValue(g.Key, out var nome) ? nome : $"Profissional #{g.Key}",
                g.Sum(x => x.Valor),
                g.Count()))
            .ToList();

        var porForma = entradasAgendamento
            .GroupBy(l => l.Descricao.Contains("presencial", StringComparison.OrdinalIgnoreCase)
                ? "Presencial"
                : "Outro")
            .Select(g => new RelatorioPorFormaPagamentoDto(g.Key, g.Sum(x => x.Valor), g.Count()))
            .ToList();

        return new RelatorioAnaliticoResponseDto(
            faturamento,
            atendimentos,
            ticketMedio,
            porProfissional,
            porForma);
    }

    public async Task<FluxoCaixaResponseDto> ObterFluxoCaixaAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var inicio = filtro.Inicio ?? DateTime.UtcNow.Date.AddDays(-30);
        var fim = filtro.Fim ?? DateTime.UtcNow.Date.AddDays(1);

        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(caixa.Id, inicio, fim),
            cancellationToken);

        var dias = new List<FluxoCaixaDiaDto>();
        decimal saldoAcumulado = caixa.SaldoTotal;

        for (var dia = inicio.Date; dia < fim.Date; dia = dia.AddDays(1))
        {
            var proximoDia = dia.AddDays(1);
            var doDia = lancamentos
                .Where(l => l.CreateAd >= dia && l.CreateAd < proximoDia)
                .ToList();

            var entradas = doDia
                .Where(l => LancamentoCaixaClassificador.EhEntrada(l.Tipo))
                .Sum(l => l.Valor);
            var saidas = doDia
                .Where(l => LancamentoCaixaClassificador.EhSaida(l.Tipo))
                .Sum(l => l.Valor);
            var saldoInicialDia = saldoAcumulado;
            saldoAcumulado += entradas - saidas;

            dias.Add(new FluxoCaixaDiaDto(dia, saldoInicialDia, entradas, saidas, saldoAcumulado));
        }

        var projecao = await CalcularProjecaoReceitaAsync(estabelecimentoId, cancellationToken);

        return new FluxoCaixaResponseDto(
            caixa.SaldoTotal,
            dias,
            saldoAcumulado,
            projecao);
    }

    public async Task<string> ExportarRelatorioCsvAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var lancamentos = await ListarRelatorioAsync(estabelecimentoId, filtro, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Id,Tipo,Valor,Descricao,AgendamentoId,CriadoEm");

        foreach (var l in lancamentos)
        {
            sb.AppendLine(string.Join(',',
                l.Id,
                Csv(l.Tipo),
                l.Valor.ToString(CultureInfo.InvariantCulture),
                Csv(l.Descricao),
                l.AgendamentoId?.ToString() ?? string.Empty,
                l.CriadoEm.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    public async Task<FinanceiroExportacaoResponseDto> ExportarRelatorioAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        string formato,
        CancellationToken cancellationToken = default)
    {
        var normalizado = (formato ?? "csv").Trim().ToLowerInvariant();
        if (normalizado is "csv")
        {
            var csv = await ExportarRelatorioCsvAsync(estabelecimentoId, filtro, cancellationToken);
            return new FinanceiroExportacaoResponseDto(
                Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"relatorio-financeiro-{estabelecimentoId}.csv");
        }

        var lancamentos = await ListarRelatorioAsync(estabelecimentoId, filtro, cancellationToken);

        if (normalizado is "xlsx")
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Relatorio");
            sheet.Cell(1, 1).Value = "Id";
            sheet.Cell(1, 2).Value = "Tipo";
            sheet.Cell(1, 3).Value = "Valor";
            sheet.Cell(1, 4).Value = "Descricao";
            sheet.Cell(1, 5).Value = "AgendamentoId";
            sheet.Cell(1, 6).Value = "CriadoEm";
            var row = 2;
            foreach (var l in lancamentos)
            {
                sheet.Cell(row, 1).Value = l.Id;
                sheet.Cell(row, 2).Value = l.Tipo;
                sheet.Cell(row, 3).Value = l.Valor;
                sheet.Cell(row, 4).Value = l.Descricao;
                sheet.Cell(row, 5).Value = l.AgendamentoId?.ToString() ?? string.Empty;
                sheet.Cell(row, 6).Value = l.CriadoEm;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new FinanceiroExportacaoResponseDto(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"relatorio-financeiro-{estabelecimentoId}.xlsx");
        }

        if (normalizado is "pdf")
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Relatorio Financeiro").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Data");
                            header.Cell().Text("Tipo");
                            header.Cell().Text("Valor");
                            header.Cell().Text("Descricao");
                        });
                        foreach (var l in lancamentos)
                        {
                            table.Cell().Text(l.CriadoEm.ToString("dd/MM/yyyy"));
                            table.Cell().Text(l.Tipo);
                            table.Cell().Text(l.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR")));
                            table.Cell().Text(l.Descricao);
                        }
                    });
                });
            }).GeneratePdf();

            return new FinanceiroExportacaoResponseDto(
                pdf,
                "application/pdf",
                $"relatorio-financeiro-{estabelecimentoId}.pdf");
        }

        throw new LancamentoCaixaInvalidoException("Formato de exportacao invalido.");
    }

    public async Task<FinanceiroBuscaResponseDto> BuscarAsync(
        int estabelecimentoId,
        string termo,
        string? tipo,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var q = termo.Trim();
        if (q.Length < 2)
        {
            return new FinanceiroBuscaResponseDto(
                Array.Empty<LancamentoCaixaResponseDto>(),
                Array.Empty<ContaReceberResponseDto>(),
                Array.Empty<ContaPagarResponseDto>());
        }

        var tipoNorm = (tipo ?? string.Empty).Trim().ToLowerInvariant();
        var incluirLancamentos = string.IsNullOrEmpty(tipoNorm) || tipoNorm is "lancamento" or "lancamentos";
        var incluirReceber = string.IsNullOrEmpty(tipoNorm) || tipoNorm is "receber" or "conta-receber";
        var incluirPagar = string.IsNullOrEmpty(tipoNorm) || tipoNorm is "pagar" or "conta-pagar";

        IReadOnlyList<LancamentoCaixaResponseDto> lancamentos = Array.Empty<LancamentoCaixaResponseDto>();
        if (incluirLancamentos)
        {
            var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
            var lista = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
                new LancamentoCaixaFiltro(caixa.Id, null, null),
                cancellationToken);
            lancamentos = lista
                .Where(l =>
                    l.Descricao.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || l.Valor.ToString(CultureInfo.InvariantCulture).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (l.AgendamentoId?.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(50)
                .Select(LancamentoCaixaResponseDto.From)
                .ToList();
        }

        IReadOnlyList<ContaReceberResponseDto> contasReceber = Array.Empty<ContaReceberResponseDto>();
        if (incluirReceber)
        {
            var contas = await _contaReceberRepository.ListarPorEstabelecimentoAsync(
                estabelecimentoId,
                null,
                cancellationToken);
            contasReceber = contas
                .Where(c => c.Descricao.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Take(50)
                .Select(MapearContaReceber)
                .ToList();
        }

        IReadOnlyList<ContaPagarResponseDto> contasPagar = Array.Empty<ContaPagarResponseDto>();
        if (incluirPagar)
        {
            var contas = await _contaPagarRepository.ListarPorEstabelecimentoAsync(
                estabelecimentoId,
                null,
                cancellationToken);
            contasPagar = contas
                .Where(c =>
                    c.Descricao.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || c.Fornecedor.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || c.Categoria.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Take(50)
                .Select(MapearContaPagar)
                .ToList();
        }

        return new FinanceiroBuscaResponseDto(lancamentos, contasReceber, contasPagar);
    }

    public async Task AtualizarContasVencidasAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        await AtualizarVencidasContaReceberAsync(hoje, cancellationToken);
        await AtualizarVencidasContaPagarAsync(hoje, cancellationToken);
    }

    private async Task AtualizarVencidasContaReceberAsync(DateTime hoje, CancellationToken cancellationToken)
    {
        var abertas = await _contaReceberRepository.ListarAbertasVencidasAsync(hoje, cancellationToken);
        foreach (var conta in abertas)
        {
            conta.Status = ContaFinanceiraStatus.Vencida;
            conta.UpdatedAt = DateTime.UtcNow;
            _contaReceberRepository.Atualizar(conta);
        }

        if (abertas.Count > 0)
        {
            await _contaReceberRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task AtualizarVencidasContaPagarAsync(DateTime hoje, CancellationToken cancellationToken)
    {
        var abertas = await _contaPagarRepository.ListarAbertasVencidasAsync(hoje, cancellationToken);
        foreach (var conta in abertas)
        {
            conta.Status = ContaFinanceiraStatus.Vencida;
            conta.UpdatedAt = DateTime.UtcNow;
            _contaPagarRepository.Atualizar(conta);
        }

        if (abertas.Count > 0)
        {
            await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ContaReceberResponseDto>> ListarContasReceberAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var contas = await _contaReceberRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            status,
            cancellationToken);

        return contas.Select(MapearContaReceber).ToList();
    }

    public async Task<ContaReceberResponseDto> CriarContaReceberAsync(
        int estabelecimentoId,
        CriarContaReceberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        if (request.Valor <= 0)
        {
            throw new LancamentoCaixaInvalidoException("Valor da conta a receber invalido.");
        }

        var conta = new ContaReceber
        {
            EstabelecimentoId = estabelecimentoId,
            AgendamentoId = request.AgendamentoId,
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            Status = ContaFinanceiraStatus.Aberta,
            CreateAd = DateTime.UtcNow
        };

        await _contaReceberRepository.AdicionarAsync(conta, cancellationToken);
        await _contaReceberRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaReceberCriada,
            nameof(ContaReceber),
            conta.Id,
            request,
            cancellationToken);

        return MapearContaReceber(conta);
    }

    public async Task<ContaReceberResponseDto> BaixarContaReceberAsync(
        int estabelecimentoId,
        int contaId,
        BaixarContaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var conta = await _contaReceberRepository.ObterPorIdEEstabelecimentoAsync(
            contaId,
            estabelecimentoId,
            cancellationToken);

        if (conta is null)
        {
            throw new LancamentoCaixaInvalidoException("Conta a receber nao encontrada.");
        }

        if (conta.Status != ContaFinanceiraStatus.Aberta && conta.Status != ContaFinanceiraStatus.Vencida)
        {
            throw new LancamentoCaixaInvalidoException("Conta a receber nao esta aberta.");
        }

        var obs = string.IsNullOrWhiteSpace(request.Observacao) ? string.Empty : $" ({request.Observacao.Trim()})";
        var forma = string.IsNullOrWhiteSpace(request.FormaBaixa) ? string.Empty : $" [{request.FormaBaixa.Trim()}]";
        var lancamento = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.AjusteManual,
                conta.Valor,
                $"Baixa conta a receber #{conta.Id}: {conta.Descricao}{forma}{obs}",
                AgendamentoId: conta.AgendamentoId),
            cancellationToken);

        conta.Status = ContaFinanceiraStatus.Paga;
        conta.LancamentoCaixaId = lancamento.Id;
        conta.UpdatedAt = DateTime.UtcNow;

        _contaReceberRepository.Atualizar(conta);
        await _contaReceberRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaReceberBaixada,
            nameof(ContaReceber),
            conta.Id,
            new { contaId = conta.Id, lancamentoId = lancamento.Id },
            cancellationToken);

        return MapearContaReceber(conta);
    }

    public async Task<ContaReceberResponseDto> CancelarContaReceberAsync(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var conta = await _contaReceberRepository.ObterPorIdEEstabelecimentoAsync(
            contaId,
            estabelecimentoId,
            cancellationToken);

        if (conta is null)
        {
            throw new LancamentoCaixaInvalidoException("Conta a receber nao encontrada.");
        }

        if (conta.Status is ContaFinanceiraStatus.Paga or ContaFinanceiraStatus.Cancelada)
        {
            throw new LancamentoCaixaInvalidoException("Conta a receber nao pode ser cancelada.");
        }

        conta.Status = ContaFinanceiraStatus.Cancelada;
        conta.UpdatedAt = DateTime.UtcNow;

        _contaReceberRepository.Atualizar(conta);
        await _contaReceberRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaReceberCancelada,
            nameof(ContaReceber),
            conta.Id,
            new { contaId = conta.Id },
            cancellationToken);

        return MapearContaReceber(conta);
    }

    public async Task<IReadOnlyList<ContaPagarResponseDto>> ListarContasPagarAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var contas = await _contaPagarRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            status,
            cancellationToken);

        return contas.Select(MapearContaPagar).ToList();
    }

    public async Task<ContaPagarResponseDto> CriarContaPagarAsync(
        int estabelecimentoId,
        CriarContaPagarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        if (request.Valor <= 0)
        {
            throw new LancamentoCaixaInvalidoException("Valor da conta a pagar invalido.");
        }

        var conta = new ContaPagar
        {
            EstabelecimentoId = estabelecimentoId,
            Fornecedor = request.Fornecedor.Trim(),
            Categoria = request.Categoria.Trim(),
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            Recorrente = request.Recorrente,
            Status = ContaFinanceiraStatus.Aberta,
            CreateAd = DateTime.UtcNow
        };

        await _contaPagarRepository.AdicionarAsync(conta, cancellationToken);
        await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaPagarCriada,
            nameof(ContaPagar),
            conta.Id,
            request,
            cancellationToken);

        return MapearContaPagar(conta);
    }

    public async Task<ContaPagarResponseDto> BaixarContaPagarAsync(
        int estabelecimentoId,
        int contaId,
        BaixarContaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var conta = await _contaPagarRepository.ObterPorIdEEstabelecimentoAsync(
            contaId,
            estabelecimentoId,
            cancellationToken);

        if (conta is null)
        {
            throw new LancamentoCaixaInvalidoException("Conta a pagar nao encontrada.");
        }

        if (conta.Status != ContaFinanceiraStatus.Aberta && conta.Status != ContaFinanceiraStatus.Vencida)
        {
            throw new LancamentoCaixaInvalidoException("Conta a pagar nao esta aberta.");
        }

        var obs = string.IsNullOrWhiteSpace(request.Observacao) ? string.Empty : $" ({request.Observacao.Trim()})";
        var forma = string.IsNullOrWhiteSpace(request.FormaBaixa) ? string.Empty : $" [{request.FormaBaixa.Trim()}]";
        var lancamento = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.Saque,
                conta.Valor,
                $"Baixa conta a pagar #{conta.Id}: {conta.Descricao}{forma}{obs}"),
            cancellationToken);

        conta.Status = ContaFinanceiraStatus.Paga;
        conta.LancamentoCaixaId = lancamento.Id;
        conta.UpdatedAt = DateTime.UtcNow;

        _contaPagarRepository.Atualizar(conta);
        await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaPagarBaixada,
            nameof(ContaPagar),
            conta.Id,
            new { contaId = conta.Id, lancamentoId = lancamento.Id },
            cancellationToken);

        return MapearContaPagar(conta);
    }

    public async Task<ContaPagarResponseDto> CancelarContaPagarAsync(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var conta = await _contaPagarRepository.ObterPorIdEEstabelecimentoAsync(
            contaId,
            estabelecimentoId,
            cancellationToken);

        if (conta is null)
        {
            throw new LancamentoCaixaInvalidoException("Conta a pagar nao encontrada.");
        }

        if (conta.Status is ContaFinanceiraStatus.Paga or ContaFinanceiraStatus.Cancelada)
        {
            throw new LancamentoCaixaInvalidoException("Conta a pagar nao pode ser cancelada.");
        }

        conta.Status = ContaFinanceiraStatus.Cancelada;
        conta.UpdatedAt = DateTime.UtcNow;

        _contaPagarRepository.Atualizar(conta);
        await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ContaPagarCancelada,
            nameof(ContaPagar),
            conta.Id,
            new { contaId = conta.Id },
            cancellationToken);

        return MapearContaPagar(conta);
    }

    public async Task<IReadOnlyList<ConciliacaoItemResponseDto>> ListarConciliacaoAsync(
        int estabelecimentoId,
        bool? conciliado,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var itens = await _conciliacaoItemRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            conciliado,
            cancellationToken);

        return itens.Select(MapearConciliacao).ToList();
    }

    public async Task<IReadOnlyList<ConciliacaoItemResponseDto>> ImportarConciliacaoCsvAsync(
        int estabelecimentoId,
        ConciliacaoImportacaoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var lancamentos = await _lancamentoCaixaRepository.ListarPorCaixaAsync(
            new LancamentoCaixaFiltro(caixa.Id, null, null),
            cancellationToken);

        var resultados = new List<ConciliacaoItemResponseDto>();

        foreach (var linha in request.Linhas)
        {
            var match = lancamentos.FirstOrDefault(l =>
                l.Valor == linha.Valor
                && l.CreateAd.Date == linha.Data.Date
                && l.ConciliacaoStatus == ConciliacaoStatus.Pendente);

            var item = new ConciliacaoItem
            {
                EstabelecimentoId = estabelecimentoId,
                LancamentoCaixaId = match?.Id,
                DescricaoExtrato = linha.Descricao,
                ValorExtrato = linha.Valor,
                DataExtrato = linha.Data,
                ReferenciaExtrato = linha.Referencia,
                Conciliado = match is not null,
                CreateAd = DateTime.UtcNow
            };

            await _conciliacaoItemRepository.AdicionarAsync(item, cancellationToken);

            if (match is not null)
            {
                var lancamentoTracked = await _lancamentoCaixaRepository.ObterPorIdAsync(
                    match.Id,
                    cancellationToken);

                if (lancamentoTracked is not null)
                {
                    lancamentoTracked.ConciliacaoStatus = ConciliacaoStatus.Conciliado;
                    _lancamentoCaixaRepository.Atualizar(lancamentoTracked);
                }
            }

            resultados.Add(MapearConciliacao(item));
        }

        await _conciliacaoItemRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ConciliacaoImportada,
            nameof(ConciliacaoItem),
            estabelecimentoId,
            new { quantidade = request.Linhas.Count },
            cancellationToken);

        return resultados;
    }

    private async Task<IReadOnlyList<ComissaoProfissionalResponseDto>> ListarComissoesInternoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var vinculoIds = vinculos.Select(v => v.Id).ToList();

        if (vinculoIds.Count == 0)
        {
            return Array.Empty<ComissaoProfissionalResponseDto>();
        }

        var comissoes = await _comissaoProfissionalRepository.ListarAtivasPorVinculosAsync(
            vinculoIds,
            cancellationToken);
        var vinculoPorId = vinculos.ToDictionary(v => v.Id);

        return comissoes
            .Where(c => vinculoPorId.ContainsKey(c.ProfissionalEstabelecimentoId))
            .Select(c => ComissaoProfissionalResponseDto.From(c, vinculoPorId[c.ProfissionalEstabelecimentoId]))
            .ToList();
    }

    private async Task<decimal?> CalcularProjecaoReceitaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddDays(30);
        var quantidade = await _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
            estabelecimentoId,
            inicio,
            fim,
            cancellationToken);

        return quantidade > 0 ? quantidade * 100m : null;
    }

    private static void ValidarComissaoRequest(
        TipoComissao tipo,
        decimal? percentual,
        decimal? valorFixo)
    {
        if (tipo == TipoComissao.Percentual && (!percentual.HasValue || percentual <= 0))
        {
            throw new ComissaoProfissionalInvalidaException("Percentual obrigatorio para comissao percentual.");
        }

        if (tipo == TipoComissao.ValorFixo && (!valorFixo.HasValue || valorFixo <= 0))
        {
            throw new ComissaoProfissionalInvalidaException("Valor fixo obrigatorio para comissao fixa.");
        }

        if (tipo == TipoComissao.Mista
            && ((!percentual.HasValue || percentual <= 0) || (!valorFixo.HasValue || valorFixo <= 0)))
        {
            throw new ComissaoProfissionalInvalidaException("Percentual e valor fixo obrigatorios para comissao mista.");
        }
    }

    private static ContaReceberResponseDto MapearContaReceber(ContaReceber conta) =>
        new(
            conta.Id,
            conta.EstabelecimentoId,
            conta.AgendamentoId,
            conta.Descricao,
            conta.Valor,
            conta.Vencimento,
            conta.Status.ToString());

    private static ContaPagarResponseDto MapearContaPagar(ContaPagar conta) =>
        new(
            conta.Id,
            conta.EstabelecimentoId,
            conta.Fornecedor,
            conta.Categoria,
            conta.Descricao,
            conta.Valor,
            conta.Vencimento,
            conta.Recorrente,
            conta.Status.ToString());

    private static ConciliacaoItemResponseDto MapearConciliacao(ConciliacaoItem item) =>
        new(
            item.Id,
            item.LancamentoCaixaId,
            item.DescricaoExtrato,
            item.ValorExtrato,
            item.DataExtrato,
            item.Conciliado);

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private async Task<Caixa> ObterCaixaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken) =>
        await _caixaRepository.ObterOuProvisionarPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
}
