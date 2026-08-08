using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class ModulosAssinaturaService : IModulosAssinaturaService
{
    private static readonly IReadOnlyList<ModuloAssinatura> ModulosBaseTenant =
    [
        ModuloAssinatura.Estabelecimento,
        ModuloAssinatura.Assinatura
    ];

    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;

    public ModulosAssinaturaService(
        IAssinaturaRepository assinaturaRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository)
    {
        _assinaturaRepository = assinaturaRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
    }

    public async Task<ModulosAssinaturaResponseDto> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var assinatura = await _assinaturaRepository.ObterAssinaturaEfetivaPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        var profissionalAutonomoId = await ResolverProfissionalAutonomoIdAsync(
            assinatura,
            estabelecimentoId,
            cancellationToken);

        return CriarResposta(assinatura, estabelecimentoId, profissionalAutonomoId);
    }

    public async Task<bool> PossuiModuloPorEstabelecimentoAsync(
        int estabelecimentoId,
        ModuloAssinatura modulo,
        CancellationToken cancellationToken = default)
    {
        var modulos = await ObterPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        return modulos.AssinaturaAtiva && modulos.Modulos.Contains(modulo.ToString());
    }

    private static ModulosAssinaturaResponseDto CriarResposta(
        Assinatura? assinatura,
        int estabelecimentoId,
        int? profissionalAutonomoId)
    {
        if (assinatura is null
            || assinatura.Status is not (
                AssinaturaStatus.Ativa
                or AssinaturaStatus.Trial
                or AssinaturaStatus.Inadimplente
                or AssinaturaStatus.CancelamentoAgendado))
        {
            return ModulosAssinaturaResponseDto.Bloqueado(assinatura, estabelecimentoId);
        }

        var tipo = assinatura.TipoAssinatura;
        var modulosDoPlano = PlanoComercialCatalogo.Obter(assinatura.Plano, tipo).Modulos;
        var modulosLiberados = ModulosBaseTenant
            .Concat(modulosDoPlano)
            .Distinct()
            .ToList();

        return ModulosAssinaturaResponseDto.Liberado(
            assinatura,
            estabelecimentoId,
            modulosLiberados,
            profissionalAutonomoId);
    }

    private async Task<int?> ResolverProfissionalAutonomoIdAsync(
        Assinatura? assinatura,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (assinatura?.TipoAssinatura != TipoAssinatura.ProfissionalAutonomo)
        {
            return null;
        }

        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return vinculos
            .Select(v => v.Profissional)
            .FirstOrDefault(p => p is { TipoProfissional: ProfessionalType.Autonomo, Ativo: true })
            ?.Id;
    }
}
