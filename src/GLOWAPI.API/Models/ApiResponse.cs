namespace GLOWAPI.API.Models;

public record ApiErrorResponse(bool Success, string Message, string Code)
{
    public static ApiErrorResponse From(string message, string code) =>
        new(false, message, code);
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
