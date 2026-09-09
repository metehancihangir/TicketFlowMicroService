namespace TicketFlow.EventService.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Used for optimistic concurrency in Faz 4
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}
