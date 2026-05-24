namespace GLOWAPI.API.Middlewares;

/// <summary>
/// Stub preparado para RBAC/permissões futuras.
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context) => _next(context);
}
