using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

// Rate Limiting (Dakikada 10 Ä°stek)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ReservationPolicy", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // AÅŸÄ±m durumunda direkt reddet (429)
    });
    options.RejectionStatusCode = 429;
});

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS â€” only Gateway defines CORS (Angular origin)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting(); // Endpoint'i tanimak icin sart
app.UseRateLimiter(); // Routing sonrasi, endpoint match olunca calisir

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Gateway" }));

// YARP - forward all matched routes
app.MapReverseProxy();

app.Run();

// For integration tests
public partial class Program { }

