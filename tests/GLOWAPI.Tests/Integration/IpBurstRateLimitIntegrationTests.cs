using System.Net;
using System.Net.Http.Json;
using GLOWAPI.API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class IpBurstRateLimitIntegrationTests : IClassFixture<RateLimitWebApplicationFactory>
{
    private readonly RateLimitWebApplicationFactory _factory;

    public IpBurstRateLimitIntegrationTests(RateLimitWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QuartaRequisicaoEmBurst_DeveRetornar429EIpBlocked24H()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        for (var i = 0; i < 3; i++)
        {
            var ok = await client.GetAsync("/api/planos");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var blocked = await client.GetAsync("/api/planos");
        Assert.Equal((HttpStatusCode)429, blocked.StatusCode);

        var body = await blocked.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("IP_BLOCKED_24H", body!.Code);

        var stillBlocked = await client.GetAsync("/api/planos");
        Assert.Equal((HttpStatusCode)429, stillBlocked.StatusCode);
    }

    [Fact]
    public async Task Healthcheck_NaoDeveContarNoRateLimit()
    {
        using var factory = new RateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

public class RateLimitWebApplicationFactory : GlowApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:Enabled"] = "true",
                ["RateLimit:BurstMaxRequests"] = "3",
                ["RateLimit:BurstWindowSeconds"] = "30",
                ["RateLimit:BlockDurationHours"] = "24"
            });
        });
    }
}
