using TicketFlow.ReservationService.Models;

namespace TicketFlow.ReservationService.DTOs;

public record CreateReservationRequest(Guid EventId, int SeatCount);

public record ReservationResponse(
    Guid Id,
    Guid UserId,
    Guid EventId,
    int SeatCount,
    ReservationStatus Status,
    PaymentStatus PaymentStatus,
    DateTime CreatedAt
);
