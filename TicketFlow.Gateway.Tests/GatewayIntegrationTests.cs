using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TicketFlow.Gateway.Tests;

public class GatewayIntegrationTests
{
    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:auth-cluster:Destinations:d1:Address"] = "http://localhost:19999",
                    ["ReverseProxy:Clusters:event-cluster:Destinations:d1:Address"] = "http://localhost:19999",
                    ["ReverseProxy:Clusters:reservation-cluster:Destinations:d1:Address"] = "http://localhost:19999",
                    ["ReverseProxy:Clusters:analytics-cluster:Destinations:d1:Address"] = "http://localhost:19999"
                });
            });
        });
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/unknown/resource");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AuthRoute_IsProxied_NotFound404()
    {
        // /api/auth/* should be matched by a YARP route (not return 404).
        // Since the upstream (localhost:19999) is unreachable, we get 502 BadGateway.
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "test@test.com", password = "pw" });
        // Route IS configured -> should NOT be 404
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
