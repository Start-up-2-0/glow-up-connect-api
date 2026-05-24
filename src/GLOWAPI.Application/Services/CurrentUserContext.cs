using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class CurrentUserContext : ICurrentUserContext
{
    public int? UserId { get; private set; }
    public string? Email { get; private set; }
    public UserRole? Role { get; private set; }
    public int? SessionId { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void Set(int userId, string email, UserRole role, int sessionId)
    {
        UserId = userId;
        Email = email;
        Role = role;
        SessionId = sessionId;
    }
}
