using MassTransit;
using MongoDB.Driver;
using TicketFlow.AnalyticsService.Models;
using TicketFlow.Contracts;

namespace TicketFlow.AnalyticsService.Consumers;

public class TicketReservedConsumer : IConsumer<TicketReservedEvent>
{
    private readonly IMongoCollection<EventAnalytics> _collection;
    private readonly ILogger<TicketReservedConsumer> _logger;

    public TicketReservedConsumer(IMongoDatabase database, ILogger<TicketReservedConsumer> logger)
    {
        _collection = database.GetCollection<EventAnalytics>("EventAnalytics");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketReservedEvent> context)
    {
        var msg = context.Message;

        var filter = Builders<EventAnalytics>.Filter.Eq(x => x.EventId, msg.EventId);
        var update = Builders<EventAnalytics>.Update
            .Inc(x => x.TotalTicketsSold, msg.SeatCount);

        var options = new FindOneAndUpdateOptions<EventAnalytics>
        {
            IsUpsert = true
        };

        await _collection.FindOneAndUpdateAsync(filter, update, options);
        _logger.LogInformation("TotalTicketsSold incremented for EventId {EventId} by {Count}", msg.EventId, msg.SeatCount);
    }
}
