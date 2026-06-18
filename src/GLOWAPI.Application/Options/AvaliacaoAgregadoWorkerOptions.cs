namespace GLOWAPI.Application.Options;

public class AvaliacaoAgregadoWorkerOptions
{
    public const string SectionName = "AvaliacaoAgregadoWorker";

    public bool Habilitado { get; set; } = true;
    public int IntervaloProcessamentoMs { get; set; } = 3_600_000;
}
