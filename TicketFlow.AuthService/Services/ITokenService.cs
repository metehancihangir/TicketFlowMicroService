using TicketFlow.AuthService.Models;

namespace TicketFlow.AuthService.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    DateTime GetExpiry();
}
