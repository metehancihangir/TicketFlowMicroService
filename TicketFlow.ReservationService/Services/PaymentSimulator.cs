using TicketFlow.ReservationService.Models;

namespace TicketFlow.ReservationService.Services;

public class PaymentSimulator : IPaymentSimulator
{
    private readonly Random _rng = new();

    public async Task<PaymentStatus> SimulateAsync()
    {
        // Simulate payment gateway latency
        await Task.Delay(500);

        // 5% chance of payment failure (for testing purposes)
        return _rng.Next(100) < 5 ? PaymentStatus.Failed : PaymentStatus.Completed;
    }
}
