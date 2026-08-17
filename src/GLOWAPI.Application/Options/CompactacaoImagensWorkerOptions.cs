namespace GLOWAPI.Application.Options;

public class CompactacaoImagensWorkerOptions
{
    public const string SectionName = "CompactacaoImagensWorker";

    public bool Habilitado { get; set; }
    public int TamanhoLote { get; set; } = 50;
    public int IntervaloProcessamentoMs { get; set; } = 60_000;
}
