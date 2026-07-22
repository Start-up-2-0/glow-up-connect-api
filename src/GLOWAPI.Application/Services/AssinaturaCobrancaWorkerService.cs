using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class AssinaturaCobrancaWorkerService : IAssinaturaCobrancaWorkerService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly ICobrancaAssinaturaService _cobrancaAssinaturaService;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;
    private readonly IAssinaturaTitularContatoService _assinaturaTitularContatoService;
    private readonly IAssinaturaEncerramentoService _assinaturaEncerramentoService;

    public AssinaturaCobrancaWorkerService(
        IAssinaturaRepository assinaturaRepository,
        ICobrancaAssinaturaService cobrancaAssinaturaService,
        IAssinaturaNotificacaoService assinaturaNotificacaoService,
        IAssinaturaTitularContatoService assinaturaTitularContatoService,
        IAssinaturaEncerramentoService assinaturaEncerramentoService)
    {
        _assinaturaRepository = assinaturaRepository;
        _cobrancaAssinaturaService = cobrancaAssinaturaService;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
        _assinaturaTitularContatoService = assinaturaTitularContatoService;
        _assinaturaEncerramentoService = assinaturaEncerramentoService;
    }

    public async Task ProcessarCicloDiarioAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow;

        await ProcessarAlertasAsync(hoje, cancellationToken);
        await ProcessarGeracaoCobrancasAsync(hoje, cancellationToken);
        await _cobrancaAssinaturaService.MarcarAtrasadasAsync(cancellationToken);
        await _assinaturaEncerramentoService.ProcessarCancelamentosAgendadosAsync(hoje, cancellationToken);
        await _cobrancaAssinaturaService.EncerrarInadimplentesAsync(cancellationToken);
    }

    private async Task ProcessarAlertasAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken)
    {
        var assinaturas = await _assinaturaRepository.ListarParaAlertaFaturaAsync(dataReferenciaUtc, cancellationToken);

        foreach (var assinatura in assinaturas)
        {
            var titular = await _assinaturaTitularContatoService.ResolverAsync(assinatura, cancellationToken);
            await _assinaturaNotificacaoService.AlertaFaturaProximaAsync(
                assinatura,
                titular.Email,
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
