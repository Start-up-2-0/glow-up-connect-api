namespace GLOWAPI.Application.Options;

public class ExclusaoContaOptions
{
    public const string SectionName = "ExclusaoConta";

    public int DiasCarencia { get; set; } = 30;
}
