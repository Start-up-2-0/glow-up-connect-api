namespace GLOWAPI.Application.Options;

public class GeocodificacaoOptions
{
    public const string SectionName = "Geocodificacao:Nominatim";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public string UserAgent { get; set; } = "GlowUpConnectAPI/1.0";
    public string PaisPadrao { get; set; } = "br";
    public int TimeoutSegundos { get; set; } = 5;
}
