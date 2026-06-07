namespace GLOWAPI.Application.Options;

public class AssinaturaCobrancaOptions
{
    public const string SectionName = "AssinaturaCobranca";

    public int DiasAntecedenciaGeracaoCobranca { get; set; } = 2;
    public int DiasAntecedenciaAlertaFatura { get; set; } = 3;
    public int[] DiasVencimentoPermitidos { get; set; } = [5, 10, 15, 20];
}
