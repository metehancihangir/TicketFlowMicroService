using MassTransit;
using TicketFlow.TicketWorker.Consumers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, loggerConfig) =>
    loggerConfig.WriteTo.Console().ReadFrom.Configuration(builder.Configuration));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketReservedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("ticket-reserved-queue", e =>
        {
            e.ConfigureConsumer<TicketReservedConsumer>(ctx);
        });
    });
});

var app = builder.Build();
app.Run();

public partial class Program { }
