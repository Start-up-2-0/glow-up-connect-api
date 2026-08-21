using System.Text.Json;
using GLOWAPI.API.Attributes;
using GLOWAPI.API.Middlewares;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.AspNetCore.Http;
using Moq;

namespace GLOWAPI.Tests.Unit.API;

public class PermissionMiddlewareTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();

    [Fact]
    public async Task InvokeAsync_SemRequisitoDeModulo_DeveContinuarPipeline()
    {
        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext();

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ComRequisitoSemUsuarioAutenticado_DeveRetornar401()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(false);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.Estabelecimento,
                ModuloAssinatura.Agenda,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("UNAUTHORIZED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComModuloLiberadoParaEstabelecimento_DeveContinuarPipeline()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Agenda,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.Estabelecimento,
                ModuloAssinatura.Agenda,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ComModuloBloqueado_DeveRetornar403()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Caixa,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.Estabelecimento,
                ModuloAssinatura.Caixa,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("SUBSCRIPTION_MODULE_BLOCKED", body.GetProperty("code").GetString());
        Assert.Equal("Caixa", body.GetProperty("details").GetProperty("modulo").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComParametroInvalido_DeveRetornar400()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.Estabelecimento,
                ModuloAssinatura.Agenda,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "abc";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("INVALID_SUBSCRIPTION_SCOPE", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComModuloLiberadoParaTipoAutonomo_DeveValidarComoEstabelecimento()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                20,
                ModuloAssinatura.Servicos,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.ProfissionalAutonomo,
                ModuloAssinatura.Servicos,
                "profissionalId"));
        context.Request.RouteValues["profissionalId"] = "20";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ComPermissaoLiberada_DeveContinuarPipeline()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                10,
                PermissaoNegocio.EquipeGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarResultadoAutorizacao(PermissaoNegocio.EquipeGerenciar));

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.EquipeGerenciar,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ComPermissaoNegada_DeveRetornar403()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                10,
                PermissaoNegocio.CaixaVisualizar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.CaixaVisualizar,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal(UsuarioSemPermissaoNegocioException.ErrorCode, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComPermissaoSemVinculo_DeveRetornar403()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                10,
                PermissaoNegocio.AgendaVisualizarGeral,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemVinculoNegocioException());

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.AgendaVisualizarGeral,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal(UsuarioSemVinculoNegocioException.ErrorCode, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComParametroNegocioInvalido_DeveRetornar400()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CriarHttpContext(
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.AgendaVisualizarGeral,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "abc";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await LerCorpoJson(context);
        Assert.Equal("INVALID_BUSINESS_SCOPE", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ComPermissaoPorPublicGuid_DeveAutorizarPorGuid()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        var publicGuid = Guid.NewGuid();
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarPorPublicGuidAsync(
                publicGuid,
                PermissaoNegocio.NegocioEditar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarResultadoAutorizacao(PermissaoNegocio.NegocioEditar));

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.NegocioEditar,
                "publicGuid",
                parametroEhPublicGuid: true));
        context.Request.RouteValues["publicGuid"] = publicGuid.ToString();

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ComModuloEPermissaoLiberados_DeveValidarAmbosEContinuar()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Profissionais,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                10,
                PermissaoNegocio.EquipeGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarResultadoAutorizacao(PermissaoNegocio.EquipeGerenciar));

        var called = false;
        var middleware = CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = CriarHttpContext(
            new RequerModuloAssinaturaAttribute(
                TipoAssinatura.Estabelecimento,
                ModuloAssinatura.Profissionais,
                "estabelecimentoId"),
            new RequerPermissaoNegocioAttribute(
                PermissaoNegocio.EquipeGerenciar,
                "estabelecimentoId"));
        context.Request.RouteValues["estabelecimentoId"] = "10";

        await middleware.InvokeAsync(
            context,
            _currentUserContext.Object,
            _modulosAssinaturaService.Object,
            _autorizacaoNegocioService.Object);

        Assert.True(called);
        _modulosAssinaturaService.Verify(s => s.PossuiModuloPorEstabelecimentoAsync(
            10,
            ModuloAssinatura.Profissionais,
            It.IsAny<CancellationToken>()), Times.Once);
        _autorizacaoNegocioService.Verify(s => s.AutorizarAsync(
            10,
            PermissaoNegocio.EquipeGerenciar,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PermissionMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next);

    private static DefaultHttpContext CriarHttpContext(params object[] metadata)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(metadata),
            "test"));

        return context;
    }

    private static async Task<JsonElement> LerCorpoJson(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    private static AutorizacaoNegocioResultado CriarResultadoAutorizacao(
        params PermissaoNegocio[] permissoes) =>
        new(
            EstabelecimentoId: 10,
            UsuarioId: 20,
            Role: EstablishmentUserRole.Owner,
            PossuiVinculoProfissional: false,
            Permissoes: permissoes.ToHashSet());
}
