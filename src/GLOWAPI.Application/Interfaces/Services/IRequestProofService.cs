using GLOWAPI.Application.Models.Security;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IRequestProofService
{
    IReadOnlyList<string> EmitirProofs(string contextId, string? method, string? path, int count);

    RequestProofValidationResult ValidarEConsumir(
        string? proofHeader,
        string method,
        string path,
        string? contextId);
}
