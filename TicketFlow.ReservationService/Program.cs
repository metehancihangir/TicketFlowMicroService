using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.ReservationService.Data;
using TicketFlow.ReservationService.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservationDb")));

// JWT Auth (stateless, same key as AuthService)
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Payment simulator
builder.Services.AddSingleton<IPaymentSimulator, PaymentSimulator>();

// Named HttpClient for Event Service
builder.Services.AddHttpClient("EventService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:EventService"] ?? "http://localhost:5002");
});

var app = builder.Build();

// Auto-migrate on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ReservationService" }));
app.MapControllers();

app.Run();

// For integration tests
public partial class Program { }
