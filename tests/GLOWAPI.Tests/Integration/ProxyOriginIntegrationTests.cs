using System.Net;
using System.Net.Http.Json;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GLOWAPI.Application.Interfaces.Services;
using Moq;

namespace GLOWAPI.Tests.Integration;

public class ProxyOriginIntegrationTests
{
  private const string ProxySecret = "integration-proxy-secret";

    [Fact]
    public async Task RotaApp_SemProxySecret_DeveRetornar403()
    {
        using var factory = CreateFactory(proxyEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/planos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("PROXY_ORIGIN_REQUIRED", body!.Code);
    }

    [Fact]
    public async Task RotaApp_ComProxySecret_DevePermitir()
    {
        using var factory = CreateFactory(proxyEnabled: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProxyOriginOptions.SecretHeaderName, ProxySecret);

        var response = await client.GetAsync("/api/planos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_SemProxySecret_DevePermitir()
    {
        using var factory = CreateFactory(proxyEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_SemProxySecret_DevePermitirChegarNoController()
    {
        using var factory = CreateFactory(proxyEnabled: true);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/webhooks/whatsapp", new { apikey = "invalida" });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool proxyEnabled)
    {
        return new GlowApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GLOW_PROXY_SECRET"] = ProxySecret,
                    ["ProxyOrigin:Enabled"] = proxyEnabled.ToString().ToLowerInvariant(),
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
