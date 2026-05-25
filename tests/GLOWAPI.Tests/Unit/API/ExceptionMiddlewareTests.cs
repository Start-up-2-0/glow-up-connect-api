using System.Text.Json;
using GLOWAPI.API.Middlewares;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GLOWAPI.Tests.Unit.API;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DeveRetornarFormatoPadronizado_ParaTokenExpirado()
    {
        var middleware = new ExceptionMiddleware(_ => throw new TokenExpiredException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("TOKEN_EXPIRED", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_DeveRetornarForbidden_ParaUsuarioBloqueado()
    {
        var middleware = new ExceptionMiddleware(_ => throw new UserBlockedException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DeveRetornarNotFound_ParaUsuarioNaoEncontrado()
    {
        var middleware = new ExceptionMiddleware(_ => throw new UsuarioNaoEncontradoException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("USUARIO_NAO_ENCONTRADO", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_DeveRetornarForbidden_ParaEmailNaoConfirmado()
    {
        var middleware = new ExceptionMiddleware(_ => throw new EmailNaoConfirmadoException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DeveRetornarBadRequest_ParaAvatarInvalido()
    {
        var middleware = new ExceptionMiddleware(_ => throw new AvatarInvalidoException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DeveRetornarConflict_ParaEmailJaCadastrado()
    {
        var middleware = new ExceptionMiddleware(_ => throw new EmailJaCadastradoException(), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }
}
