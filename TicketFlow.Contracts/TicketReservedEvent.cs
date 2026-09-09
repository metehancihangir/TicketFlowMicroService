namespace TicketFlow.Contracts;

/// <summary>
/// Published by ReservationService when a reservation is successfully created and payment completed.
/// Consumed by TicketWorker to generate the ticket.
/// </summary>
public record TicketReservedEvent(
    Guid ReservationId,
    Guid UserId,
    Guid EventId,
    int SeatCount,
    DateTime ReservedAt
);
