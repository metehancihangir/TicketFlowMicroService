using TicketFlow.ReservationService.Models;

namespace TicketFlow.ReservationService.Services;

public interface IPaymentSimulator
{
    Task<PaymentStatus> SimulateAsync();
}
