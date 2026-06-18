using System.Text.Json;
using GLOWAPI.API.Middlewares;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.API;

public class GlowTokenAuthenticationMiddlewareTests
{
    private readonly Mock<IAuthSessionService> _authSessionService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<ISecurityAuditLogger> _auditLogger = new();
    private readonly AuthOptions _authOptions = new() { TokenHeaderName = "x-glow-token", SlidingRenewalMinutes = 30 };

    private GlowTokenAuthenticationMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, Options.Create(_authOptions));

    [Fact]
    public async Task InvokeAsync_WebhookEvolutionComAllowAnonymous_DevePassarSemToken()
    {
        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var context = CriarHttpContext(comAllowAnonymous: true);
        context.Request.Path = "/api/webhooks/whatsapp/evolution/messages-upsert";

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RotaPublica_DevePassarSemToken()
    {
        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var context = CriarHttpContext(comAllowAnonymous: true);

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RotaPrivadaSemHeader_DeveRetornar401()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(comAllowAnonymous: false);

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("UNAUTHORIZED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_TokenInvalido_DeveRetornar401()
    {
        _authSessionService
            .Setup(s => s.ObterSessaoAtivaPorAccessTokenAsync("bad", It.IsAny<AuthSessionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidTokenException());

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(comAllowAnonymous: false, token: "bad");

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("INVALID_TOKEN", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_TokenExpirado_DeveRetornar401()
    {
        _authSessionService
            .Setup(s => s.ObterSessaoAtivaPorAccessTokenAsync("expired", It.IsAny<AuthSessionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TokenExpiredException());

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(comAllowAnonymous: false, token: "expired");

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        var body = await LerCorpoJson(context);
        Assert.Equal("TOKEN_EXPIRED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_TokenValido_DevePopularCurrentUserContext()
    {
        var auth = new AuthenticatedSessionResult(
            new SessaoAutenticacaoInfo(10, 1, "127.0.0.1", "agent"),
            new UsuarioAuthInfo(1, "Teste", "test@email.com", UserRole.Cliente));

        _authSessionService
            .Setup(s => s.ObterSessaoAtivaPorAccessTokenAsync("valid", It.IsAny<AuthSessionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(auth);
        _authSessionService
            .Setup(s => s.ObterSessaoPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Domain.Entities.SessaoAutenticacao
            {
                Id = 10,
                LoginEm = DateTime.UtcNow,
                UltimaRenovacaoEm = DateTime.UtcNow
            });

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(comAllowAnonymous: false, token: "valid");

        await middleware.InvokeAsync(context, _authSessionService.Object, _currentUserContext.Object, _auditLogger.Object);

        Assert.True(called);
        _currentUserContext.Verify(c => c.Set(1, "test@email.com", UserRole.Cliente, 10), Times.Once);
    }

    private static DefaultHttpContext CriarHttpContext(bool comAllowAnonymous, string? token = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (!string.IsNullOrWhiteSpace(token))
        {
            context.Request.Headers["x-glow-token"] = token;
        }

        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        context.Request.Headers.UserAgent = "test-agent";

        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            comAllowAnonymous ? new EndpointMetadataCollection(new AllowAnonymousAttribute()) : EndpointMetadataCollection.Empty,
            "test");

        context.SetEndpoint(endpoint);
        return context;
    }

    private static async Task<JsonElement> LerCorpoJson(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }
}
