using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Driver;
using TicketFlow.AnalyticsService.Consumers;
using TicketFlow.AnalyticsService.Models;
using TicketFlow.Contracts;
using Xunit;

namespace TicketFlow.AnalyticsService.Tests;

public class AnalyticsTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;

    public AnalyticsTests()
    {
        // Start Mongo2Go in-memory database
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase("TestAnalyticsDb");
    }

    [Fact]
    public async Task TicketReservedConsumer_ShouldIncrementTotalTicketsSold_WhenMultipleEventsArrive()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddLogging(l => l.AddConsole())
            .AddSingleton(_database)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TicketReservedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var eventId = Guid.NewGuid();

        var msg1 = new TicketReservedEvent(
            ReservationId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            EventId: eventId,
            SeatCount: 2,
            ReservedAt: DateTime.UtcNow
        );

        var msg2 = new TicketReservedEvent(
            ReservationId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            EventId: eventId,
            SeatCount: 3,
            ReservedAt: DateTime.UtcNow
        );

        // Act
        await harness.Bus.Publish(msg1);
        await harness.Bus.Publish(msg2);

        // Wait a bit for processing
        await Task.Delay(500);

        // Assert
        Assert.True(await harness.Consumed.Any<TicketReservedEvent>());

        var collection = _database.GetCollection<EventAnalytics>("EventAnalytics");
        var doc = await collection.Find(x => x.EventId == eventId).FirstOrDefaultAsync();

        Assert.NotNull(doc);
        // First message +2 seats, second message +3 seats = 5
        Assert.Equal(5, doc.TotalTicketsSold);
    }

    public void Dispose()
    {
        _runner.Dispose();
    }
}
