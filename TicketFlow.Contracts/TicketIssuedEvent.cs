namespace TicketFlow.Contracts;

/// <summary>
/// Published by TicketWorker after ticket is generated (QR, PDF simulation, email simulation).
/// Consumed by AnalyticsService (Faz 6) to update metrics.
/// </summary>
public record TicketIssuedEvent(
    Guid TicketId,
    Guid ReservationId,
    Guid UserId,
    Guid EventId,
    string QrCode,
    DateTime IssuedAt
);
