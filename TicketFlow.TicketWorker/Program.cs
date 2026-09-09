using MassTransit;
using TicketFlow.TicketWorker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

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
