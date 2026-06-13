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
            Limites: CriarLimites(assinatura?.Plano));

    public static ModulosAssinaturaResponseDto Liberado(
        Assinatura assinatura,
        int estabelecimentoId,
        IReadOnlyList<ModuloAssinatura> modulos) =>
        new(
            AssinaturaAtiva: true,
            AssinaturaId: assinatura.Id,
            PlanoId: assinatura.PlanoId,
            PlanoNome: assinatura.Plano?.Nome,
            Status: assinatura.Status.ToString(),
            TipoAssinatura: ObterTipoAssinatura(assinatura),
            EstabelecimentoId: estabelecimentoId,
            ProfissionalAutonomoId: null,
            Modulos: modulos.Select(modulo => modulo.ToString()).ToList(),
            Limites: CriarLimites(assinatura.Plano));

    private static LimitesAssinaturaDto CriarLimites(Plano? plano)
    {
        var perfil = PlanoComercialCatalogo.Obter(plano);

        return new(
            plano?.LimiteProfissionais,
            plano?.LimiteServicos,
            plano?.LimiteAgendamentos,
            perfil.LimiteUsuarios,
            perfil.LimiteAgendamentosPorDia,
            plano?.LimiteEstabelecimentos,
            perfil.PrioridadeListagemPublica);
    }

    private static string? ObterTipoAssinatura(Assinatura? assinatura)
    {
        if (assinatura is null)
        {
            return null;
        }

        return GLOWAPI.Domain.Enums.TipoAssinatura.Estabelecimento.ToString();
    }
}
