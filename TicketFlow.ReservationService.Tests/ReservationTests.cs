using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.EventService.Data;
using TicketFlow.EventService.Models;
using TicketFlow.ReservationService.Data;
using TicketFlow.ReservationService.Models;
using TicketFlow.ReservationService.Services;

namespace TicketFlow.ReservationService.Tests;

// ─── Deterministic payment simulators ───────────────────────────────────────

public class AlwaysSucceedPayment : IPaymentSimulator
{
    public Task<PaymentStatus> SimulateAsync() =>
        Task.FromResult(PaymentStatus.Completed);
}

public class AlwaysFailPayment : IPaymentSimulator
{
    public Task<PaymentStatus> SimulateAsync() =>
        Task.FromResult(PaymentStatus.Failed);
}

// ─── JWT helper ─────────────────────────────────────────────────────────────

public static class JwtHelper
{
    private const string Key = "TicketFlow_SuperSecret_Dev_Key_MinLength32Chars!";

    public static string Generate(Guid userId, string role = "User")
    {
        var sk = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var creds = new SigningCredentials(sk, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"{userId}@test.com"),
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
}

// ─── EventService factory ────────────────────────────────────────────────────

public class EventServiceFactory : WebApplicationFactory<TicketFlow.EventService.Controllers.EventsController>
{
    private readonly string _dbName;
    public EventServiceFactory(string dbName) { _dbName = dbName; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
            services.AddDbContext<EventDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

// ─── ReservationService factory ─────────────────────────────────────────────

public class ReservationServiceFactory : WebApplicationFactory<TicketFlow.ReservationService.Controllers.ReservationsController>
{
    private readonly string _dbName;
    private readonly IPaymentSimulator _payment;
    private readonly HttpMessageHandler _eventHandler;
    private readonly Uri _eventBaseAddress;

    public ReservationServiceFactory(
        string dbName,
        IPaymentSimulator payment,
        HttpMessageHandler eventHandler,
        Uri eventBaseAddress)
    {
        _dbName = dbName;
        _payment = payment;
        _eventHandler = eventHandler;
        _eventBaseAddress = eventBaseAddress;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Replace DB
            var toRemove = services
                .Where(d => d.ServiceType.FullName != null &&
                            (d.ServiceType.FullName.Contains("DbContext") ||
                             d.ServiceType.FullName.Contains("EntityFramework") ||
                             d.ServiceType.FullName.Contains("Npgsql")))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<ReservationDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Replace payment simulator with deterministic one
            services.AddSingleton(_payment);

            // Replace EventService HttpClient — use the TestServer's handler
            services.AddHttpClient("EventService")
                .ConfigurePrimaryHttpMessageHandler(() => _eventHandler)
                .ConfigureHttpClient(c => c.BaseAddress = _eventBaseAddress);
        });
    }
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class ReservationTests
{
    // ── 1. userId comes from JWT, not body ──────────────────────────────────
    [Fact]
    public async Task CreateReservation_UsesUserIdFromJwt_NotBody()
    {
        var eventDbName = "JwtUserId_Event_" + Guid.NewGuid();
        var resDbName   = "JwtUserId_Res_"   + Guid.NewGuid();
        var eventId     = Guid.NewGuid();
        var jwtUserId   = Guid.NewGuid();

        using var eventFactory = new EventServiceFactory(eventDbName);
        // Seed event
        using (var scope = eventFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            db.Events.Add(new Event
            {
                Id = eventId, Title = "Test", Venue = "V",
                EventDate = DateTime.UtcNow.AddDays(1),
                TotalSeats = 10, AvailableSeats = 10
            });
            await db.SaveChangesAsync();
        }

        // Get the TestServer's internal handler + base address
        var eventHandler = eventFactory.Server.CreateHandler();
        var eventBaseAddress = eventFactory.Server.BaseAddress;

        using var resFactory = new ReservationServiceFactory(
            resDbName, new AlwaysSucceedPayment(), eventHandler, eventBaseAddress);

        var resClient = resFactory.CreateClient();
        resClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtHelper.Generate(jwtUserId));

        var resp = await resClient.PostAsJsonAsync("/api/reservations",
            new { eventId, seatCount = 1 });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        // Verify saved reservation has JWT userId
        using var scope2 = resFactory.Services.CreateScope();
        var resDb = scope2.ServiceProvider.GetRequiredService<ReservationDbContext>();
        var saved = await resDb.Reservations.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(jwtUserId, saved!.UserId);
    }

    // ── 2. PaymentFailed → seats compensated ────────────────────────────────
    [Fact]
    public async Task PaymentFailed_SeatsReturnedToEventService()
    {
        var eventDbName = "PayFail_Event_" + Guid.NewGuid();
        var resDbName   = "PayFail_Res_"   + Guid.NewGuid();
        var eventId     = Guid.NewGuid();
        var userId      = Guid.NewGuid();

        using var eventFactory = new EventServiceFactory(eventDbName);

        using (var scope = eventFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            db.Events.Add(new Event
            {
                Id = eventId, Title = "T", Venue = "V",
                EventDate = DateTime.UtcNow.AddDays(1),
                TotalSeats = 5, AvailableSeats = 5
            });
            await db.SaveChangesAsync();
        }

        var eventHandler     = eventFactory.Server.CreateHandler();
        var eventBaseAddress = eventFactory.Server.BaseAddress;

        using var resFactory = new ReservationServiceFactory(
            resDbName, new AlwaysFailPayment(), eventHandler, eventBaseAddress);

        var resClient = resFactory.CreateClient();
        resClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtHelper.Generate(userId));

        var resp = await resClient.PostAsJsonAsync("/api/reservations",
            new { eventId, seatCount = 2 });

        // Payment fails → 422 Unprocessable
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);

        // Seats should be returned (compensated) — check AvailableSeats = 5
        using var scope2 = eventFactory.Services.CreateScope();
        var evDb = scope2.ServiceProvider.GetRequiredService<EventDbContext>();
        var ev = await evDb.Events.FindAsync(eventId);
        Assert.NotNull(ev);
        Assert.Equal(5, ev!.AvailableSeats);
    }

    // ── 3. Race condition: 2 parallel requests for last 1 seat ──────────────
    [Fact]
    public async Task RaceCondition_OnlyOneReservationSucceeds()
    {
        var eventDbName = "Race_Event_" + Guid.NewGuid();
        var res1DbName  = "Race_Res1_"  + Guid.NewGuid();
        var res2DbName  = "Race_Res2_"  + Guid.NewGuid();
        var eventId     = Guid.NewGuid();
        var user1       = Guid.NewGuid();
        var user2       = Guid.NewGuid();

        using var eventFactory = new EventServiceFactory(eventDbName);

        using (var scope = eventFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            db.Events.Add(new Event
            {
                Id = eventId, Title = "T", Venue = "V",
                EventDate = DateTime.UtcNow.AddDays(1),
                TotalSeats = 1, AvailableSeats = 1  // Only 1 seat!
            });
            await db.SaveChangesAsync();
        }

        // Both reservation factories share the SAME event service test handler
        var eventHandler     = eventFactory.Server.CreateHandler();
        var eventBaseAddress = eventFactory.Server.BaseAddress;

        using var resFactory1 = new ReservationServiceFactory(
            res1DbName, new AlwaysSucceedPayment(), eventHandler, eventBaseAddress);
        using var resFactory2 = new ReservationServiceFactory(
            res2DbName, new AlwaysSucceedPayment(), eventHandler, eventBaseAddress);

        var client1 = resFactory1.CreateClient();
        client1.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtHelper.Generate(user1));

        var client2 = resFactory2.CreateClient();
        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtHelper.Generate(user2));

        // Fire both in parallel
        var t1 = client1.PostAsJsonAsync("/api/reservations", new { eventId, seatCount = 1 });
        var t2 = client2.PostAsJsonAsync("/api/reservations", new { eventId, seatCount = 1 });
        var results = await Task.WhenAll(t1, t2);

        var statuses = results.Select(r => (int)r.StatusCode).OrderBy(s => s).ToList();

        // One must succeed (201) and one must fail (409 or 422)
        Assert.Contains(201, statuses);
        Assert.True(statuses.Any(s => s == 409 || s == 422),
            $"Expected 409 or 422 but got: {string.Join(", ", statuses)}");
    }
}
