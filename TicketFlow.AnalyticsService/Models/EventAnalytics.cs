using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketFlow.AnalyticsService.Models;

public class EventAnalytics
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid EventId { get; set; }
    
    public int TotalTicketsSold { get; set; }
    
    public int TotalTicketsIssued { get; set; }
}
