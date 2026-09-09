using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.ReservationService.Data;
using TicketFlow.ReservationService.DTOs;
using TicketFlow.ReservationService.Models;
using TicketFlow.ReservationService.Services;

namespace TicketFlow.ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly ReservationDbContext _db;
    private readonly IPaymentSimulator _payment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        ReservationDbContext db,
        IPaymentSimulator payment,
        IHttpClientFactory httpClientFactory,
        ILogger<ReservationsController> logger)
    {
        _db = db;
        _payment = payment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        // userId must come from JWT, never from the body
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token." });

        // 1. Call Event Service to atomically decrement available seats
        // Forward the caller's JWT so EventService can validate it (service-to-service)
        var eventClient = _httpClientFactory.CreateClient("EventService");
        var bearerToken = HttpContext.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(bearerToken))
            eventClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", bearerToken);

        var seatResponse = await eventClient.PutAsync(
            $"/api/events/{request.EventId}/reserve-seat?count={request.SeatCount}",
            null);

        if (seatResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            return Conflict(new { message = "No seats available." });

        if (!seatResponse.IsSuccessStatusCode)
            return StatusCode(502, new { message = "Event service error." });

        // 2. Simulate payment
        var paymentStatus = await _payment.SimulateAsync();

        var reservation = new Reservation
        {
            UserId = userId,
            EventId = request.EventId,
            SeatCount = request.SeatCount,
            PaymentStatus = paymentStatus
        };

        if (paymentStatus == PaymentStatus.Completed)
        {
            reservation.Status = ReservationStatus.Reserved;
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();

            // TODO (Faz 5): Publish TicketReservedEvent to RabbitMQ here

            return CreatedAtAction(nameof(GetMy), new { },
                ToResponse(reservation));
        }
        else
        {
            // Payment failed — compensate: give back the seats
            reservation.Status = ReservationStatus.PaymentFailed;
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();

            // Compensating transaction: return seats to Event Service
            _logger.LogWarning("Payment failed for event {EventId}. Compensating seat count.", request.EventId);
            var compensateResponse = await eventClient.PutAsync(
                $"/api/events/{request.EventId}/return-seat?count={request.SeatCount}",
                null);

            if (!compensateResponse.IsSuccessStatusCode)
                _logger.LogError("Compensation failed for event {EventId}!", request.EventId);

            return UnprocessableEntity(new
            {
                message = "Payment simulation failed. Seats have been returned.",
                reservationId = reservation.Id
            });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMy()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var reservations = await _db.Reservations
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync();

        return Ok(reservations);
    }

    private static ReservationResponse ToResponse(Reservation r) =>
        new(r.Id, r.UserId, r.EventId, r.SeatCount, r.Status, r.PaymentStatus, r.CreatedAt);
}
