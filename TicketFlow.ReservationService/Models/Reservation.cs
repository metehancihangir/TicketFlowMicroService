namespace TicketFlow.ReservationService.Models;

public enum ReservationStatus
{
    Pending,
    Reserved,
    Cancelled,
    PaymentFailed
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed
}

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public int SeatCount { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
