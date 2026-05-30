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
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;

    public CaixaNegocioService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
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
