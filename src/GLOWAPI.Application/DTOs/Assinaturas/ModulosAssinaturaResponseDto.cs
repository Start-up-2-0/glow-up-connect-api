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
    public static ModulosAssinaturaResponseDto Bloqueado(Assinatura? assinatura = null) =>
        new(
            AssinaturaAtiva: false,
            AssinaturaId: assinatura?.Id,
            PlanoId: assinatura?.PlanoId,
            PlanoNome: assinatura?.Plano?.Nome,
            Status: assinatura?.Status.ToString(),
            TipoAssinatura: ObterTipoAssinatura(assinatura),
            EstabelecimentoId: assinatura?.EstabelecimentoId,
            ProfissionalAutonomoId: assinatura?.ProfissionalAutonomoId,
            Modulos: Array.Empty<string>(),
            Limites: CriarLimites(assinatura?.Plano));

    public static ModulosAssinaturaResponseDto Liberado(
        Assinatura assinatura,
        IReadOnlyList<ModuloAssinatura> modulos) =>
        new(
            AssinaturaAtiva: true,
            AssinaturaId: assinatura.Id,
            PlanoId: assinatura.PlanoId,
            PlanoNome: assinatura.Plano?.Nome,
            Status: assinatura.Status.ToString(),
            TipoAssinatura: ObterTipoAssinatura(assinatura),
            EstabelecimentoId: assinatura.EstabelecimentoId,
            ProfissionalAutonomoId: assinatura.ProfissionalAutonomoId,
            Modulos: modulos.Select(modulo => modulo.ToString()).ToList(),
            Limites: CriarLimites(assinatura.Plano));

    private static LimitesAssinaturaDto CriarLimites(Plano? plano) =>
        new(
            plano?.LimiteProfissionais,
            plano?.LimiteServicos,
            plano?.LimiteAgendamentos);

    private static string? ObterTipoAssinatura(Assinatura? assinatura)
    {
        if (assinatura is null)
        {
            return null;
        }

        return assinatura.EstabelecimentoId.HasValue
            ? GLOWAPI.Domain.Enums.TipoAssinatura.Estabelecimento.ToString()
            : GLOWAPI.Domain.Enums.TipoAssinatura.ProfissionalAutonomo.ToString();
    }
}
