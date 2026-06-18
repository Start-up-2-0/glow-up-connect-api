using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class PromocaoLancamentoService : IPromocaoLancamentoService
{
    public const string CodigoCampanhaLancamento = "lancamento-100";

    private readonly ICampanhaPromocionalRepository _campanhaPromocionalRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly AssinaturaCobrancaOptions _options;

    public PromocaoLancamentoService(
        ICampanhaPromocionalRepository campanhaPromocionalRepository,
        IAssinaturaRepository assinaturaRepository,
        IOptions<AssinaturaCobrancaOptions> options)
    {
        _campanhaPromocionalRepository = campanhaPromocionalRepository;
        _assinaturaRepository = assinaturaRepository;
        _options = options.Value;
    }

    public async Task<PromocaoLancamentoStatusDto> ObterStatusAsync(CancellationToken cancellationToken = default)
    {
        var campanha = await _campanhaPromocionalRepository.ObterAtivaPorCodigoAsync(
            CodigoCampanhaLancamento,
            cancellationToken);

        var assinaturasUtilizadas = campanha is null
            ? 0
            : await _assinaturaRepository.ContarPorCodigoCampanhaAsync(
                CodigoCampanhaLancamento,
                cancellationToken);

        var disponivel = campanha is not null
            && campanha.Ativa
            && assinaturasUtilizadas < campanha.Limite;
        var vagasRestantes = campanha is null
            ? 0
            : Math.Max(campanha.Limite - assinaturasUtilizadas, 0);

        return new PromocaoLancamentoStatusDto(
            disponivel,
            vagasRestantes,
            campanha?.DiasTrial ?? 30,
            _options.DiasVencimentoPermitidos,
            _options.DiasAntecedenciaAlertaFatura,
            _options.DiasAntecedenciaGeracaoCobranca);
    }

    public async Task<bool> EstabelecimentoJaUsouPromocaoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default) =>
        await _assinaturaRepository.ExisteComCampanhaPorEstabelecimentoAsync(
            estabelecimentoId,
            CodigoCampanhaLancamento,
            cancellationToken);

    public async Task<bool> TentarReservarVagaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        if (estabelecimentoId > 0
            && await EstabelecimentoJaUsouPromocaoAsync(estabelecimentoId, cancellationToken))
        {
            return false;
        }

        var campanha = await _campanhaPromocionalRepository.ObterAtivaPorCodigoAsync(
            CodigoCampanhaLancamento,
            cancellationToken);

        if (campanha is null || !campanha.Ativa)
        {
            return false;
        }

        var assinaturasUtilizadas = await _assinaturaRepository.ContarPorCodigoCampanhaAsync(
            CodigoCampanhaLancamento,
            cancellationToken);

        if (assinaturasUtilizadas >= campanha.Limite)
        {
            return false;
        }

        return await _campanhaPromocionalRepository.TentarReservarVagaAsync(
            CodigoCampanhaLancamento,
            cancellationToken);
    }
}
