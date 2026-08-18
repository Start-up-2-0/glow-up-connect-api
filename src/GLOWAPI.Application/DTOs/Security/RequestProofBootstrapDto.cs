namespace GLOWAPI.Application.DTOs.Security;

public class RequestProofBootstrapDto
{
    public IReadOnlyList<string> Proofs { get; set; } = [];
}
