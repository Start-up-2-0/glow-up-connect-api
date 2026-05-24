using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICurrentUserContext
{
    int? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    int? SessionId { get; }
    bool IsAuthenticated { get; }

    void Set(int userId, string email, UserRole role, int sessionId);
}
