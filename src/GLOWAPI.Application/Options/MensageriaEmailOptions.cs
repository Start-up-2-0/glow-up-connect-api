namespace GLOWAPI.Application.Options;

public class MensageriaEmailOptions
{
    public const string SectionName = "Mensageria:Email";

    public string Provedor { get; set; } = "resend";
    public bool Habilitado { get; set; }
    public string From { get; set; } = string.Empty;
}
