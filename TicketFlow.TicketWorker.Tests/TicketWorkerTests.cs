using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketFlow.Contracts;
using TicketFlow.TicketWorker.Consumers;
using Xunit;

namespace TicketFlow.TicketWorker.Tests;

public class TicketWorkerTests
{
    [Fact]
    public async Task TicketReservedConsumer_ShouldPublishTicketIssuedEvent()
    {
        await using var provider = new ServiceCollection()
            .AddLogging(l => l.AddConsole())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TicketReservedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // 1. Publish TicketReservedEvent
        await harness.Bus.Publish(new TicketReservedEvent(
            ReservationId: reservationId,
            UserId: userId,
            EventId: eventId,
            SeatCount: 1,
            ReservedAt: DateTime.UtcNow
        ));

        // 2. Ensure consumer consumed the message
        Assert.True(await harness.Consumed.Any<TicketReservedEvent>());

        // 3. Ensure consumer published TicketIssuedEvent
        Assert.True(await harness.Published.Any<TicketIssuedEvent>());
    }

    [Fact]
    public async Task TicketReservedConsumer_Idempotency_ShouldNotProcessDuplicateMessages()
    {
        await using var provider = new ServiceCollection()
            .AddLogging(l => l.AddConsole())
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TicketReservedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var msg = new TicketReservedEvent(
            ReservationId: reservationId,
            UserId: userId,
            EventId: eventId,
            SeatCount: 1,
            ReservedAt: DateTime.UtcNow
        );

        // 1. Publish FIRST message
        await harness.Bus.Publish(msg);
        Assert.True(await harness.Consumed.Any<TicketReservedEvent>());

        // Clear the published messages so we can count the second time
        // Note: ITestHarness doesn't let us easily clear, but we can count them
        var publishedCount1 = harness.Published.Select<TicketIssuedEvent>().Count();
        Assert.Equal(1, publishedCount1);

        // 2. Publish SECOND duplicate message
        await harness.Bus.Publish(msg);
        
        // Let it process
        await Task.Delay(200);

        var publishedCount2 = harness.Published.Select<TicketIssuedEvent>().Count();
        
        // Count should STILL be 1 because of idempotency
        Assert.Equal(1, publishedCount2);
    }
}
