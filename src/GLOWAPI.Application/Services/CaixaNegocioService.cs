using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class CaixaNegocioService : ICaixaNegocioService
{
    private readonly ICaixaRepository _caixaRepository;
    private readonly ILancamentoCaixaRepository _lancamentoCaixaRepository;
    private readonly IMovimentacaoCaixaService _movimentacaoCaixaService;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;

    public CaixaNegocioService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        IMovimentacaoCaixaService movimentacaoCaixaService,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
        _movimentacaoCaixaService = movimentacaoCaixaService;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
    }

    public async Task<CaixaResumoResponseDto> ObterResumoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.CaixaResumoConsultado,
            nameof(GLOWAPI.Domain.Entities.Caixa),
            caixa.Id,
            new { caixa.Id },
            cancellationToken);

        return CaixaResumoResponseDto.From(caixa);
    }

    public async Task<IReadOnlyList<LancamentoCaixaResponseDto>> ListarLancamentosAsync(
        int estabelecimentoId,
        LancamentoCaixaFiltroDto filtro,
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
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.CaixaLancamentosConsultados,
            nameof(GLOWAPI.Domain.Entities.LancamentoCaixa),
            caixa.Id,
            new
            {
                caixaId = caixa.Id,
                filtro.Inicio,
                filtro.Fim,
                quantidade = lancamentos.Count
            },
            cancellationToken);

        return lancamentos.Select(LancamentoCaixaResponseDto.From).ToList();
    }

    public async Task<LancamentoCaixaResponseDto> RegistrarAjusteManualAsync(
        int estabelecimentoId,
        RegistrarAjusteCaixaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        if (request.Valor <= 0)
        {
            throw new LancamentoCaixaInvalidoException("O valor do ajuste deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            throw new LancamentoCaixaInvalidoException("A descricao do ajuste e obrigatoria.");
        }

        var tipo = LancamentoCaixaClassificador.ResolverTipoAjuste(request.Subtipo);
        var descricao = $"{request.Subtipo}: {request.Descricao.Trim()}";

        var lancamento = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(tipo, request.Valor, descricao),
            cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.CaixaLancamentoRegistrado,
            nameof(GLOWAPI.Domain.Entities.LancamentoCaixa),
            lancamento.Id,
            new
            {
                lancamento.Id,
                tipo = lancamento.Tipo.ToString(),
                request.Subtipo,
                request.Valor,
                request.Descricao
            },
            cancellationToken);

        return LancamentoCaixaResponseDto.From(lancamento);
    }

    public async Task<LancamentoCaixaResponseDto> EstornarLancamentoAsync(
        int estabelecimentoId,
        int lancamentoId,
        EstornarLancamentoCaixaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new LancamentoCaixaInvalidoException("O motivo do estorno e obrigatorio.");
        }

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var original = await _lancamentoCaixaRepository.ObterPorIdECaixaAsync(
            lancamentoId,
            caixa.Id,
            cancellationToken);

        if (original is null)
        {
            throw new LancamentoCaixaNaoEncontradoException();
        }

        if (original.Tipo == LancamentoCaixaTipo.Estorno)
        {
            throw new LancamentoCaixaInvalidoException("Nao e possivel estornar um lancamento de estorno.");
        }

        var estorno = await _movimentacaoCaixaService.RegistrarLancamentoAsync(
            estabelecimentoId,
            new RegistrarLancamentoCaixaComando(
                LancamentoCaixaTipo.Estorno,
                original.Valor,
                $"Estorno: {request.Motivo.Trim()}",
                AgendamentoId: original.AgendamentoId,
                PagamentoId: original.PagamentoId,
                ProfissionalId: original.ProfissionalId,
                LancamentoOriginalId: original.Id),
            cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.CaixaLancamentoEstornado,
            nameof(GLOWAPI.Domain.Entities.LancamentoCaixa),
            estorno.Id,
            new
            {
                estorno.Id,
                lancamentoOriginalId = original.Id,
                request.Motivo
            },
            cancellationToken);

        return LancamentoCaixaResponseDto.From(estorno);
    }

    private async Task<GLOWAPI.Domain.Entities.Caixa> ObterCaixaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var caixa = await _caixaRepository.ObterPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        if (caixa is null)
        {
            throw new CaixaNegocioNaoEncontradoException();
        }

        return caixa;
    }
}
