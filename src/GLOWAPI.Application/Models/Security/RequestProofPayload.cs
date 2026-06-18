namespace GLOWAPI.Application.Models.Security;

public class RequestProofPayload
{
    public long Ts { get; set; }

    public string Nonce { get; set; } = string.Empty;

    public string M { get; set; } = string.Empty;

    public string P { get; set; } = string.Empty;

    public string Ctx { get; set; } = string.Empty;
}
