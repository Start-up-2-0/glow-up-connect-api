namespace GLOWAPI.Application.Options;

public class RetencaoDadosOptions
{
    public const string SectionName = "RetencaoDados";

    public bool Habilitado { get; set; } = true;
    public int DiasRetencao { get; set; } = 30;
    public int IntervaloProcessamentoMs { get; set; } = 3_600_000;
}
