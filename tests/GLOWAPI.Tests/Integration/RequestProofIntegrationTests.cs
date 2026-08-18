using System.Net;
using System.Net.Http.Json;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Security;
using GLOWAPI.Application.Options;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GLOWAPI.Application.Interfaces.Services;
using Moq;

namespace GLOWAPI.Tests.Integration;

public class RequestProofIntegrationTests
{
    private const string ProxySecret = "integration-proxy-secret";
    private const string RequestProofSecret = "integration-request-proof-secret-min-32!!";

    [Fact]
    public async Task RotaApp_ComProxySecret_SemProof_DeveRetornar403()
    {
        using var factory = CreateFactory(requestProofEnabled: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProxyOriginOptions.SecretHeaderName, ProxySecret);

        var response = await client.GetAsync("/api/planos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("REQUEST_PROOF_AUSENTE", body!.Code);
    }

    [Fact]
    public async Task Bootstrap_SemProof_DeveRetornarProofs()
    {
        using var factory = CreateFactory(requestProofEnabled: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProxyOriginOptions.SecretHeaderName, ProxySecret);

        var response = await client.GetAsync("/api/security/request-proof?count=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<RequestProofBootstrapDto>>();
        Assert.NotNull(body?.Data?.Proofs);
        Assert.Single(body.Data.Proofs);
    }

    [Fact]
    public async Task RotaApp_ComProofValido_DevePermitir()
    {
        using var factory = CreateFactory(requestProofEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ProxyOriginOptions.SecretHeaderName, ProxySecret);

        var bootstrap = await client.GetAsync("/api/security/request-proof?count=1");
        var bootstrapBody = await bootstrap.Content.ReadFromJsonAsync<ApiSuccessResponse<RequestProofBootstrapDto>>();
        var proof = bootstrapBody!.Data!.Proofs[0];

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/planos");
        request.Headers.Add(RequestProofOptions.DefaultHeaderName, proof);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RotaApp_ReusoDoProof_DeveRetornar403Replay()
    {
        using var factory = CreateFactory(requestProofEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ProxyOriginOptions.SecretHeaderName, ProxySecret);

        var bootstrap = await client.GetAsync("/api/security/request-proof?count=1");
        var bootstrapBody = await bootstrap.Content.ReadFromJsonAsync<ApiSuccessResponse<RequestProofBootstrapDto>>();
        var proof = bootstrapBody!.Data!.Proofs[0];

        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/planos");
        request1.Headers.Add(RequestProofOptions.DefaultHeaderName, proof);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request1)).StatusCode);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/planos");
        request2.Headers.Add(RequestProofOptions.DefaultHeaderName, proof);
        var replay = await client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        var body = await replay.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("REQUEST_PROOF_REPLAY", body!.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool requestProofEnabled)
    {
        return new GlowApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GLOW_PROXY_SECRET"] = ProxySecret,
                    ["ProxyOrigin:Enabled"] = "true",
                    ["REQUEST_PROOF_SECRET"] = RequestProofSecret,
                    ["RequestProof:Enabled"] = requestProofEnabled.ToString().ToLowerInvariant(),
                    ["Captcha:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ICaptchaValidator));
                services.AddSingleton<ICaptchaValidator>(_ => Mock.Of<ICaptchaValidator>(v =>
                    v.ValidarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)));
            });
        });
    }
}
