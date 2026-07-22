using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICicloCobrancaAssinaturaService
{
    CicloCobrancaDatasDto CalcularPrimeiroCiclo(
        DateTime dataReferenciaCiclo,
        DateTime referenciaUtc,
        PlanoPeriodo periodo);

    CicloCobrancaDatasDto CalcularProximoCiclo(DateTime vencimentoAtual, PlanoPeriodo periodo);

    void AplicarCicloNaAssinatura(Assinatura assinatura, CicloCobrancaDatasDto ciclo);

    DateTime CalcularFimTrial(DateTime inicioUtc, int diasTrial);
}
