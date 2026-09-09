using MassTransit;
using MongoDB.Driver;
using TicketFlow.AnalyticsService.Models;
using TicketFlow.Contracts;

namespace TicketFlow.AnalyticsService.Consumers;

public class TicketIssuedConsumer : IConsumer<TicketIssuedEvent>
{
    private readonly IMongoCollection<EventAnalytics> _collection;
    private readonly ILogger<TicketIssuedConsumer> _logger;

    public TicketIssuedConsumer(IMongoDatabase database, ILogger<TicketIssuedConsumer> logger)
    {
        _collection = database.GetCollection<EventAnalytics>("EventAnalytics");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketIssuedEvent> context)
    {
        var msg = context.Message;

        var filter = Builders<EventAnalytics>.Filter.Eq(x => x.EventId, msg.EventId);
        var update = Builders<EventAnalytics>.Update
            .Inc(x => x.TotalTicketsIssued, 1); // 1 ticket per event

        var options = new FindOneAndUpdateOptions<EventAnalytics>
        {
            IsUpsert = true
        };

        await _collection.FindOneAndUpdateAsync(filter, update, options);
        _logger.LogInformation("TotalTicketsIssued incremented for EventId {EventId}", msg.EventId);
    }
}
