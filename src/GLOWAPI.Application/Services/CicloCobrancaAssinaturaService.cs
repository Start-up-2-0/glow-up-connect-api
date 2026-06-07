using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class CicloCobrancaAssinaturaService : ICicloCobrancaAssinaturaService
{
    private readonly AssinaturaCobrancaOptions _options;

    public CicloCobrancaAssinaturaService(IOptions<AssinaturaCobrancaOptions> options)
    {
        _options = options.Value;
    }

    public void ValidarDiaVencimento(int diaVencimento)
    {
        if (!_options.DiasVencimentoPermitidos.Contains(diaVencimento))
        {
            throw new DiaVencimentoAssinaturaInvalidoException();
        }
    }

    public CicloCobrancaDatasDto CalcularPrimeiroCiclo(int diaVencimento, DateTime referenciaUtc) =>
        CalcularCiclo(diaVencimento, referenciaUtc);

    public CicloCobrancaDatasDto CalcularProximoCiclo(int diaVencimento, DateTime vencimentoAtual)
    {
        var referencia = vencimentoAtual.Date.AddDays(1);
        return CalcularCiclo(diaVencimento, referencia);
    }

    public void AplicarCicloNaAssinatura(Assinatura assinatura, CicloCobrancaDatasDto ciclo)
    {
        assinatura.ProximaDataVencimento = ciclo.Vencimento;
        assinatura.ProximaDataGeracaoCobranca = ciclo.Geracao;
        assinatura.ProximaDataAlerta = ciclo.Alerta;
    }

    public DateTime CalcularFimTrial(DateTime inicioUtc, int diasTrial) =>
        inicioUtc.Date.AddDays(diasTrial);

    private CicloCobrancaDatasDto CalcularCiclo(int diaVencimento, DateTime referenciaUtc)
    {
        var vencimento = CalcularProximaDataVencimento(diaVencimento, referenciaUtc);
        return new CicloCobrancaDatasDto(
            vencimento,
            vencimento.AddDays(-_options.DiasAntecedenciaGeracaoCobranca),
            vencimento.AddDays(-_options.DiasAntecedenciaAlertaFatura));
    }

    private static DateTime CalcularProximaDataVencimento(int diaVencimento, DateTime referenciaUtc)
    {
        var referencia = referenciaUtc.Date;
        var candidato = new DateTime(referencia.Year, referencia.Month, diaVencimento, 0, 0, 0, DateTimeKind.Utc);
        if (candidato < referencia)
        {
            candidato = candidato.AddMonths(1);
        }

        return candidato;
    }
}
