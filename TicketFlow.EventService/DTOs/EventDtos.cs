namespace TicketFlow.EventService.DTOs;

public record CreateEventRequest(
    string Title,
    string Venue,
    DateTime EventDate,
    int TotalSeats
);

public record EventResponse(
    Guid Id,
    string Title,
    string Venue,
    DateTime EventDate,
    int TotalSeats,
    int AvailableSeats,
    DateTime CreatedAt
);
