namespace GLOWAPI.Application.Interfaces.Services;

public interface IRequestProofNonceStore
{
    bool TryConsume(string nonce, TimeSpan ttl);
}
