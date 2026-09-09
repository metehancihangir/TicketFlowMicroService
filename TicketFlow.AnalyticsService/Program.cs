using MassTransit;
using MongoDB.Driver;
using TicketFlow.AnalyticsService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// MongoDb setup
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "TicketFlowAnalytics";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IMongoDatabase>(sp => 
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// MassTransit + RabbitMQ (Testing ortamında in-memory harness kullanılıyor olabilir)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<TicketReservedConsumer>();
        x.AddConsumer<TicketIssuedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
            cfg.Host(host, "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            
            cfg.ReceiveEndpoint("analytics-ticket-reserved", e =>
            {
                e.ConfigureConsumer<TicketReservedConsumer>(ctx);
            });
            cfg.ReceiveEndpoint("analytics-ticket-issued", e =>
            {
                e.ConfigureConsumer<TicketIssuedConsumer>(ctx);
            });
        });
    });
}

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AnalyticsService" }));
app.MapControllers();

app.Run();

// For integration tests
public partial class Program { }
