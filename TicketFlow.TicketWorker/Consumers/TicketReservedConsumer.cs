using MassTransit;
using TicketFlow.Contracts;

namespace TicketFlow.TicketWorker.Consumers;

public class TicketReservedConsumer : IConsumer<TicketReservedEvent>
{
    private readonly ILogger<TicketReservedConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    // In-memory idempotency set: stores processed reservationIds
    private static readonly HashSet<Guid> _processedReservations = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public TicketReservedConsumer(
        ILogger<TicketReservedConsumer> logger,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<TicketReservedEvent> context)
    {
        var msg = context.Message;

        // Idempotency check — if already processed, ACK and return
        await _lock.WaitAsync();
        bool alreadyProcessed;
        try { alreadyProcessed = !_processedReservations.Add(msg.ReservationId); }
        finally { _lock.Release(); }

        if (alreadyProcessed)
        {
            _logger.LogWarning("Duplicate message for ReservationId {ReservationId} — skipping.", msg.ReservationId);
            return;
        }

        _logger.LogInformation("Processing ticket for ReservationId {ReservationId}, EventId {EventId}",
            msg.ReservationId, msg.EventId);

        // Simulate QR code generation
        var qrCode = $"TF-{msg.EventId:N}-{msg.ReservationId:N}".ToUpperInvariant();
        _logger.LogInformation("QR code generated: {QrCode}", qrCode);

        // Simulate PDF generation
        _logger.LogInformation("PDF üretildi: bilet-{ReservationId}.pdf", msg.ReservationId);

        // Simulate email sending
        _logger.LogInformation("Email gönderildi: {UserId} için bilet.", msg.UserId);

        // Generate ticket and publish TicketIssuedEvent
        var ticketId = Guid.NewGuid();
        await _publishEndpoint.Publish(new TicketIssuedEvent(
            TicketId: ticketId,
            ReservationId: msg.ReservationId,
            UserId: msg.UserId,
            EventId: msg.EventId,
            QrCode: qrCode,
            IssuedAt: DateTime.UtcNow
        ));

        _logger.LogInformation("TicketIssuedEvent published for TicketId {TicketId}", ticketId);
    }
}
