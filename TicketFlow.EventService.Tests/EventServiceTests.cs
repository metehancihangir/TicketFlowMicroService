using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.EventService.Data;
using TicketFlow.EventService.DTOs;

namespace TicketFlow.EventService.Tests;

public class EventServiceTests
{
    private const string JwtKey = "TicketFlow_SuperSecret_Dev_Key_MinLength32Chars!";

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

                services.AddDbContext<EventDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
    }

    private string GenerateToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@test.com"),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        };
        var token = new JwtSecurityToken(
            issuer: "TicketFlow.AuthService",
            audience: "TicketFlow",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetEvents_Anonymous_Returns200()
    {
        using var factory = CreateFactory("GetEvents_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_NotFound_Returns404()
    {
        using var factory = CreateFactory("GetById_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NonAdmin_Returns403()
    {
        using var factory = CreateFactory("Create403_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var token = GenerateToken("User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Test Event",
            venue = "Test Venue",
            eventDate = DateTime.UtcNow.AddDays(10),
            totalSeats = 100
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Admin_ThenListContainsIt()
    {
        using var factory = CreateFactory("AdminCreate_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var token = GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResp = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Concert",
            venue = "Amphitheater",
            eventDate = DateTime.UtcNow.AddDays(30),
            totalSeats = 500
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // Reset auth header to anonymous for list
        client.DefaultRequestHeaders.Authorization = null;
        var listResp = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

        var events = await listResp.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal("Concert", events[0].Title);
    }
}
