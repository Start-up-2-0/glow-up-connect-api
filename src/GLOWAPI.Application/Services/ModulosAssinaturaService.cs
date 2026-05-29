using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class ModulosAssinaturaService : IModulosAssinaturaService
{
    private static readonly IReadOnlyList<ModuloAssinatura> ModulosEstabelecimento =
    [
        ModuloAssinatura.Estabelecimento,
        ModuloAssinatura.Assinatura
    ];

    private readonly IAssinaturaRepository _assinaturaRepository;

    public ModulosAssinaturaService(IAssinaturaRepository assinaturaRepository)
    {
        _assinaturaRepository = assinaturaRepository;
    }

    public async Task<ModulosAssinaturaResponseDto> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var assinatura = await _assinaturaRepository.ObterAtualPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return CriarResposta(assinatura, ModulosEstabelecimento);
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
        IReadOnlyList<ModuloAssinatura> modulos)
    {
        if (assinatura is null || assinatura.Status != AssinaturaStatus.Ativa)
        {
            return ModulosAssinaturaResponseDto.Bloqueado(assinatura);
        }

        var modulosDoPlano = PlanoComercialCatalogo.Obter(assinatura.Plano).Modulos;
        var modulosLiberados = modulos
            .Concat(modulosDoPlano)
            .Distinct()
            .ToList();

        return ModulosAssinaturaResponseDto.Liberado(assinatura, modulosLiberados);
    }
}
