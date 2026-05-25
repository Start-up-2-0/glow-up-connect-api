namespace GLOWAPI.Application.Options;

public class MensageriaOptions
{
    public const string SectionName = "Mensageria";

    public bool Habilitado { get; set; } = true;
    public int TamanhoLote { get; set; } = 10;
    public int IntervaloProcessamentoMs { get; set; } = 2000;
    public int MaximoTentativasPadrao { get; set; } = 5;
    public int BackoffBaseSegundos { get; set; } = 30;
    public int BackoffMaximoSegundos { get; set; } = 3600;
    public int TimeoutProcessamentoMinutos { get; set; } = 15;
    public int IntervaloRecuperacaoMs { get; set; } = 60000;
    public string InstanciaWorkerPrefixo { get; set; } = "glow-worker";
    public bool MascararDadosSensiveisEmLogs { get; set; } = true;
}
