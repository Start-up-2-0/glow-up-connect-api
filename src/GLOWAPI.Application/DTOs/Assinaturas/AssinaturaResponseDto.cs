using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record AssinaturaResponseDto(
    int Id,
    int PlanoId,
    int? EstabelecimentoId,
    int? ProfissionalAutonomoId,
    string Status,
    string Gateway,
    DateTime Inicio,
    DateTime? Fim)
{
    public static AssinaturaResponseDto From(Assinatura assinatura) =>
        new(
            assinatura.Id,
            assinatura.PlanoId,
            assinatura.EstabelecimentoId,
            assinatura.ProfissionalAutonomoId,
            assinatura.Status.ToString(),
            assinatura.Gateway.ToString(),
            assinatura.Inicio,
            assinatura.Fim);
}
