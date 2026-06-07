using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICicloCobrancaAssinaturaService
{
    void ValidarDiaVencimento(int diaVencimento);
    CicloCobrancaDatasDto CalcularPrimeiroCiclo(int diaVencimento, DateTime referenciaUtc);
    CicloCobrancaDatasDto CalcularProximoCiclo(int diaVencimento, DateTime vencimentoAtual);
    void AplicarCicloNaAssinatura(Assinatura assinatura, CicloCobrancaDatasDto ciclo);
    DateTime CalcularFimTrial(DateTime inicioUtc, int diasTrial);
}
