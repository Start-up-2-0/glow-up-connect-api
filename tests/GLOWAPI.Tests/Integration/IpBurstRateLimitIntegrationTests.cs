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
    public async Task BurstAcimaDoLimite_DeveRetornar429SoftSemBloqueioPermanente()
    {
        using var factory = new RateLimitWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        for (var i = 0; i < 3; i++)
        {
            var ok = await client.GetAsync("/api/planos");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var throttled = await client.GetAsync("/api/planos");
        Assert.Equal((HttpStatusCode)429, throttled.StatusCode);

        var body = await throttled.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("RATE_LIMIT_BURST", body!.Code);
        Assert.True(throttled.Headers.RetryAfter is not null);
    }

    [Fact]
    public async Task RequisicaoAutenticada_NaoDeveContarNoBurstGlobal()
    {
        using var factory = new RateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/planos");
            request.Headers.Add("x-glow-token", "token-presente-mas-invalido-para-rate-limit");
            var response = await client.SendAsync(request);
            Assert.NotEqual((HttpStatusCode)429, response.StatusCode);
        }
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
                ["RateLimit:BurstPenaltySeconds"] = "1",
                ["RateLimit:HardBlockMultiplier"] = "5",
                ["RateLimit:BlockDurationHours"] = "24",
                ["RateLimit:ExemptAuthenticatedRequests"] = "true",
                ["RateLimit:SensitiveMaxRequests"] = "2",
                ["RateLimit:SensitiveWindowSeconds"] = "60",
                ["RateLimit:SensitivePenaltySeconds"] = "1",
                ["RateLimit:SensitiveHardBlockMultiplier"] = "4"
            });
        });
    }
}
