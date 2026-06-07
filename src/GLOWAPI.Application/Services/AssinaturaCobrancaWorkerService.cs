using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class AssinaturaCobrancaWorkerService : IAssinaturaCobrancaWorkerService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly ICobrancaAssinaturaService _cobrancaAssinaturaService;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;

    public AssinaturaCobrancaWorkerService(
        IAssinaturaRepository assinaturaRepository,
        ICobrancaAssinaturaService cobrancaAssinaturaService,
        IAssinaturaNotificacaoService assinaturaNotificacaoService)
    {
        _assinaturaRepository = assinaturaRepository;
        _cobrancaAssinaturaService = cobrancaAssinaturaService;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
    }

    public async Task ProcessarCicloDiarioAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow;

        await ProcessarAlertasAsync(hoje, cancellationToken);
        await ProcessarGeracaoCobrancasAsync(hoje, cancellationToken);
        await _cobrancaAssinaturaService.MarcarAtrasadasAsync(cancellationToken);
    }

    private async Task ProcessarAlertasAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken)
    {
        var assinaturas = await _assinaturaRepository.ListarParaAlertaFaturaAsync(dataReferenciaUtc, cancellationToken);

        foreach (var assinatura in assinaturas)
        {
            await _assinaturaNotificacaoService.AlertaFaturaProximaAsync(
                assinatura,
                null,
                cancellationToken);

            assinatura.UltimoAlertaFaturaEm = dataReferenciaUtc;
            assinatura.UpdatedAt = DateTime.UtcNow;
            _assinaturaRepository.Atualizar(assinatura);
        }

        if (assinaturas.Count > 0)
        {
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task ProcessarGeracaoCobrancasAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken)
    {
        var assinaturas = await _assinaturaRepository.ListarParaGeracaoCobrancaAsync(dataReferenciaUtc, cancellationToken);

        foreach (var assinatura in assinaturas)
        {
            await _cobrancaAssinaturaService.GerarCobrancaRecorrenteAsync(assinatura, cancellationToken);
        }
    }
}
