using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record ModulosAssinaturaResponseDto(
    bool AssinaturaAtiva,
    int? AssinaturaId,
    int? PlanoId,
    string? PlanoNome,
    string? Status,
    string? TipoAssinatura,
    int? EstabelecimentoId,
    int? ProfissionalAutonomoId,
    IReadOnlyList<string> Modulos,
    LimitesAssinaturaDto Limites)
{
    public static ModulosAssinaturaResponseDto Bloqueado(Assinatura? assinatura = null, int? estabelecimentoId = null) =>
        new(
            AssinaturaAtiva: false,
            AssinaturaId: assinatura?.Id,
            PlanoId: assinatura?.PlanoId,
            PlanoNome: assinatura?.Plano?.Nome,
            Status: assinatura?.Status.ToString(),
            TipoAssinatura: ObterTipoAssinatura(assinatura),
            EstabelecimentoId: estabelecimentoId ?? assinatura?.EstabelecimentoId,
            ProfissionalAutonomoId: null,
            Modulos: Array.Empty<string>(),
            Limites: CriarLimites(assinatura));

    public static ModulosAssinaturaResponseDto Liberado(
        Assinatura assinatura,
        int estabelecimentoId,
        IReadOnlyList<ModuloAssinatura> modulos,
        int? profissionalAutonomoId = null) =>
        new(
            AssinaturaAtiva: true,
            AssinaturaId: assinatura.Id,
            PlanoId: assinatura.PlanoId,
            PlanoNome: assinatura.Plano?.Nome,
            Status: assinatura.Status.ToString(),
            TipoAssinatura: ObterTipoAssinatura(assinatura),
            EstabelecimentoId: estabelecimentoId,
            ProfissionalAutonomoId: profissionalAutonomoId,
            Modulos: modulos.Select(modulo => modulo.ToString()).ToList(),
            Limites: CriarLimites(assinatura));

    private static LimitesAssinaturaDto CriarLimites(Assinatura? assinatura)
    {
        var tipo = assinatura?.TipoAssinatura ?? Domain.Enums.TipoAssinatura.Estabelecimento;
        var plano = assinatura?.Plano;
        var perfil = PlanoComercialCatalogo.Obter(plano, tipo);

        return new(
            perfil.LimiteProfissionaisEfetivo ?? plano?.LimiteProfissionais,
            plano?.LimiteServicos,
            plano?.LimiteAgendamentos,
            perfil.LimiteUsuarios,
            perfil.LimiteAgendamentosPorDia,
            perfil.LimiteEstabelecimentosEfetivo ?? plano?.LimiteEstabelecimentos,
            perfil.PrioridadeListagemPublica);
    }

    private static string? ObterTipoAssinatura(Assinatura? assinatura) =>
        assinatura?.TipoAssinatura.ToString();
}
