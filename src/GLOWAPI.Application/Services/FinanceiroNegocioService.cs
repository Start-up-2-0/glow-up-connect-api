using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class FinanceiroNegocioService : IFinanceiroNegocioService
{
    private static readonly HashSet<LancamentoCaixaTipo> TiposEntrada =
    [
        LancamentoCaixaTipo.EntradaAgendamento,
        LancamentoCaixaTipo.AjusteManual
    ];

    private static readonly HashSet<LancamentoCaixaTipo> TiposSaida =
    [
        LancamentoCaixaTipo.ComissaoProfissional,
        LancamentoCaixaTipo.TaxaPlataforma,
        LancamentoCaixaTipo.Assinatura,
        LancamentoCaixaTipo.Saque,
        LancamentoCaixaTipo.Estorno,
        LancamentoCaixaTipo.Chargeback
    ];

    private readonly ICaixaRepository _caixaRepository;
    private readonly ILancamentoCaixaRepository _lancamentoCaixaRepository;
    private readonly IComissaoProfissionalRepository _comissaoProfissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public FinanceiroNegocioService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        IComissaoProfissionalRepository comissaoProfissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
        _comissaoProfissionalRepository = comissaoProfissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
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
            .Where(l => TiposEntrada.Contains(l.Tipo))
            .Sum(l => l.Valor);
        var saidas = lancamentos
            .Where(l => TiposSaida.Contains(l.Tipo))
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

    private async Task<GLOWAPI.Domain.Entities.Caixa> ObterCaixaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var caixa = await _caixaRepository.ObterPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        if (caixa is null)
        {
            throw new CaixaNegocioNaoEncontradoException();
        }

        return caixa;
    }
}
