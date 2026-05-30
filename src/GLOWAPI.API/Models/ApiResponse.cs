namespace GLOWAPI.API.Models;

public record ApiErrorResponse(bool Success, string Message, string Code, object? Details = null)
{
    public static ApiErrorResponse From(string message, string code, object? details = null) =>
        new(false, message, code, details);
}

public record ApiSuccessResponse<T>(bool Success, string Message, T? Data)
{
    public static ApiSuccessResponse<T> From(string message, T? data) =>
        new(true, message, data);
}

public record ApiSuccessResponse(bool Success, string Message)
{
    public static ApiSuccessResponse From(string message) =>
        new(true, message);
}
