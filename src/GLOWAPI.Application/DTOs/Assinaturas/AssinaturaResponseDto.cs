using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

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
    PagamentoAssinaturaResponseDto? PagamentoInicial = null,
    DateTime? DataReferenciaCiclo = null,
    DateTime? ProximaDataVencimento = null,
    DateTime? ProximaDataGeracaoCobranca = null,
    DateTime? ProximaDataAlerta = null,
    bool EmTrial = false,
    int? DiasTrial = null,
    bool RequerConfirmacaoEmail = false)
{
    public static AssinaturaResponseDto From(
        Assinatura assinatura,
        PagamentoAssinaturaResponseDto? pagamentoInicial = null,
        int? diasTrial = null,
        bool requerConfirmacaoEmail = false) =>
        new(
            assinatura.Id,
            assinatura.PlanoId,
            assinatura.PlanoAlteracaoPendenteId,
            assinatura.EstabelecimentoId,
            null,
            assinatura.Status.ToString(),
            assinatura.Gateway.ToString(),
            assinatura.Inicio,
            assinatura.Fim,
            pagamentoInicial,
            assinatura.DataReferenciaCiclo,
            assinatura.ProximaDataVencimento,
            assinatura.ProximaDataGeracaoCobranca,
            assinatura.ProximaDataAlerta,
            assinatura.Status == AssinaturaStatus.Trial,
            diasTrial,
            requerConfirmacaoEmail);
}
