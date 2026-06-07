namespace GLOWAPI.Application.Options;

public class AssinaturaCobrancaWorkerOptions
{
    public const string SectionName = "AssinaturaCobrancaWorker";

    public bool Habilitado { get; set; } = true;
    public int IntervaloProcessamentoMs { get; set; } = 300_000;
}
