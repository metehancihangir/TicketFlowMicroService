using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TicketFlow.Gateway.Tests;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReservationRoute_ShouldReturn429_WhenLimitExceeded()
    {
        var client = _factory.CreateClient();
        int totalRequests = 15;
        var responses = new List<HttpResponseMessage>();

        for (int i = 0; i < totalRequests; i++)
        {
            var response = await client.GetAsync("/api/reservations/fake-endpoint");
            responses.Add(response);
        }

        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
        Assert.NotEmpty(rateLimitedResponses);
    }
}
