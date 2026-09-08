using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.AuthService.Data;
using TicketFlow.AuthService.DTOs;

namespace TicketFlow.AuthService.Tests;

public class AuthIntegrationTests
{
    private WebApplicationFactory<Program> CreateFactory(string dbName)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType.FullName != null &&
                                (d.ServiceType.FullName.Contains("DbContext") ||
                                 d.ServiceType.FullName.Contains("EntityFramework") ||
                                 d.ServiceType.FullName.Contains("Npgsql")))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<AuthDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    [Fact]
    public async Task RegisterThenLogin_ReturnsValidToken()
    {
        using var factory = CreateFactory("IntegrationTestDb_" + Guid.NewGuid());
        var client = factory.CreateClient();

        // Register
        var registerResp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "integration@test.com", password = "Password123!" });
        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);

        // Login
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "integration@test.com", password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginData);
        Assert.False(string.IsNullOrEmpty(loginData.Token));
    }
}
