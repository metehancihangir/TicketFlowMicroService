using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TicketFlow.AnalyticsService.Models;

namespace TicketFlow.AnalyticsService.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IMongoCollection<EventAnalytics> _collection;

    public AnalyticsController(IMongoDatabase database)
    {
        _collection = database.GetCollection<EventAnalytics>("EventAnalytics");
    }

    [HttpGet("trending-events")]
    public async Task<IActionResult> GetTrendingEvents()
    {
        // TotalTicketsSold alanina gore azalan sirada sirala
        var trending = await _collection.Find(_ => true)
            .SortByDescending(x => x.TotalTicketsSold)
            .Limit(10)
            .ToListAsync();

        return Ok(trending);
    }
}
