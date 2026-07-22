namespace GLOWAPI.Application.Options;

public class AssinaturaCobrancaOptions
{
    public const string SectionName = "AssinaturaCobranca";

    public int DiasAntecedenciaGeracaoCobranca { get; set; } = 7;
    public int DiasAntecedenciaAlertaFatura { get; set; } = 7;
    public int DiasToleranciaInadimplencia { get; set; } = 10;
}
