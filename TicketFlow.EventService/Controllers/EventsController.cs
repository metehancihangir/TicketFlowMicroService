using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.EventService.Data;
using TicketFlow.EventService.DTOs;
using TicketFlow.EventService.Models;

namespace TicketFlow.EventService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventDbContext _db;

    public EventsController(EventDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .OrderBy(e => e.EventDate)
            .Select(e => new EventResponse(
                e.Id, e.Title, e.Venue, e.EventDate,
                e.TotalSeats, e.AvailableSeats, e.CreatedAt))
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e is null)
            return NotFound(new { message = $"Event {id} not found." });

        return Ok(new EventResponse(
            e.Id, e.Title, e.Venue, e.EventDate,
            e.TotalSeats, e.AvailableSeats, e.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var ev = new Event
        {
            Title = request.Title,
            Venue = request.Venue,
            EventDate = request.EventDate,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.TotalSeats
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.Id },
            new EventResponse(ev.Id, ev.Title, ev.Venue, ev.EventDate,
                ev.TotalSeats, ev.AvailableSeats, ev.CreatedAt));
    }

    // Skeleton — real atomic logic comes in Faz 4
    [HttpPut("{id:guid}/reserve-seat")]
    [Authorize]
    public async Task<IActionResult> ReserveSeat(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null)
            return NotFound(new { message = $"Event {id} not found." });

        if (ev.AvailableSeats <= 0)
            return Conflict(new { message = "No seats available." });

        // Faz 4: atomic decrement with optimistic concurrency will be implemented here
        return Ok(new { message = "Seat reserved (skeleton — Faz 4 will implement atomicity)." });
    }
}
