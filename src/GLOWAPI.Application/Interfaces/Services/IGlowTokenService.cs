using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public record GlowTokenMetadata(int UserId, int SessionId, long IssuedAtUnix, UserRole Role);

public interface IGlowTokenService
{
    string EmitirAccessToken(Usuario usuario, int sessionId, DateTime issuedAt);
    GlowTokenMetadata? ValidarMetadata(string token);
    string GerarRefreshToken();
    string HashToken(string token);
    string ProtegerToken(string token);
    string? DesprotegerToken(string tokenProtegido);
    DateTime ObterExpiracaoAccessToken(DateTime issuedAt);
    DateTime ObterExpiracaoRefreshToken(DateTime issuedAt);
    bool EstaExpirado(GlowTokenMetadata metadata, DateTime utcNow);
}
