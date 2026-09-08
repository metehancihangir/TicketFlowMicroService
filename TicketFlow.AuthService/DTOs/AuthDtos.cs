namespace TicketFlow.AuthService.DTOs;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RegisterResponse(Guid UserId, string Email);
public record LoginResponse(string Token, DateTime ExpiresAt);
