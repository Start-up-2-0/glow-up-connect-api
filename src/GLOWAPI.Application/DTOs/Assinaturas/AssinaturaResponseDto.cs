using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record AssinaturaResponseDto(
    int Id,
    int PlanoId,
    int? PlanoAlteracaoPendenteId,
    int? EstabelecimentoId,
    int? ProfissionalAutonomoId,
    string Status,
    string Gateway,
    DateTime Inicio,
    DateTime? Fim,
    PagamentoAssinaturaResponseDto? PagamentoInicial = null)
{
    public static AssinaturaResponseDto From(
        Assinatura assinatura,
        PagamentoAssinaturaResponseDto? pagamentoInicial = null) =>
        new(
            assinatura.Id,
            assinatura.PlanoId,
            assinatura.PlanoAlteracaoPendenteId,
            assinatura.EstabelecimentoId,
            assinatura.ProfissionalAutonomoId,
            assinatura.Status.ToString(),
            assinatura.Gateway.ToString(),
            assinatura.Inicio,
            assinatura.Fim,
            pagamentoInicial);
}
