using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class CicloCobrancaAssinaturaService : ICicloCobrancaAssinaturaService
{
    private readonly AssinaturaCobrancaOptions _options;

    public CicloCobrancaAssinaturaService(IOptions<AssinaturaCobrancaOptions> options)
    {
        _options = options.Value;
    }

    public CicloCobrancaDatasDto CalcularPrimeiroCiclo(
        DateTime dataReferenciaCiclo,
        DateTime referenciaUtc,
        PlanoPeriodo periodo)
    {
        var vencimento = CalcularProximaRenovacao(dataReferenciaCiclo.Date, referenciaUtc.Date, periodo);
        return CriarCicloDto(vencimento);
    }

    public CicloCobrancaDatasDto CalcularProximoCiclo(DateTime vencimentoAtual, PlanoPeriodo periodo)
    {
        var vencimento = AdicionarPeriodo(vencimentoAtual.Date, periodo);
        return CriarCicloDto(vencimento);
    }

    public CicloCobrancaDatasDto CalcularCicloPorVencimento(DateTime vencimento) =>
        CriarCicloDto(vencimento.Date);

    public void AplicarCicloNaAssinatura(Assinatura assinatura, CicloCobrancaDatasDto ciclo)
    {
        assinatura.ProximaDataVencimento = ciclo.Vencimento;
        assinatura.ProximaDataGeracaoCobranca = ciclo.Geracao;
        assinatura.ProximaDataAlerta = ciclo.Alerta;
    }

    public DateTime CalcularFimTrial(DateTime inicioUtc, int diasTrial) =>
        inicioUtc.Date.AddDays(diasTrial);

    private CicloCobrancaDatasDto CriarCicloDto(DateTime vencimento) =>
        new(
            vencimento,
            vencimento.AddDays(-_options.DiasAntecedenciaGeracaoCobranca),
            vencimento.AddDays(-_options.DiasAntecedenciaAlertaFatura));

    private static DateTime CalcularProximaRenovacao(
        DateTime dataReferenciaCiclo,
        DateTime referenciaUtc,
        PlanoPeriodo periodo)
    {
        var candidato = AdicionarPeriodo(dataReferenciaCiclo, periodo);
        while (candidato < referenciaUtc)
        {
            candidato = AdicionarPeriodo(candidato, periodo);
        }

        return candidato;
    }

    internal static DateTime AdicionarPeriodo(DateTime data, PlanoPeriodo periodo) =>
        periodo switch
        {
            PlanoPeriodo.Mensal => data.AddMonths(1),
            PlanoPeriodo.Trimestral => data.AddMonths(3),
            PlanoPeriodo.Semestral => data.AddMonths(6),
            PlanoPeriodo.Anual => data.AddYears(1),
            _ => data.AddMonths(1)
        };
}
