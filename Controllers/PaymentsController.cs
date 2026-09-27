using System.Security.Claims;
using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Payment;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Guest")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> MakePayment(
        CreatePaymentRequest request)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var reservation = await _db.Reservations
            .Include(r => r.ReservationRooms)
            .FirstOrDefaultAsync(r =>
                r.Id == request.ReservationId &&
                r.GuestId == userId);

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != "PendingPayment")
        {
            return BadRequest(new
            {
                message = "This reservation is not awaiting payment."
            });
        }

        // Check whether the payment window has expired.
        if (reservation.ExpiresAt.HasValue &&
            reservation.ExpiresAt.Value <= DateTime.UtcNow)
        {
            reservation.Status = "Expired";

            await _db.SaveChangesAsync();

            return BadRequest(new
            {
                message = "The payment window has expired. Please create a new booking."
            });
        }

        if (!request.PaymentSuccessful)
        {
            var failedPayment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalAmount,
                GatewayReference =
                    "MOCK-FAILED-" +
                    Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 10)
                        .ToUpper(),
                Status = "Failed"
            };

            _db.Payments.Add(failedPayment);

            await _db.SaveChangesAsync();

            return BadRequest(new
            {
                message = "Payment failed.",
                reservationId = reservation.Id,
                status = "PaymentFailed"
            });
        }

        var payment = new Payment
        {
            ReservationId = reservation.Id,
            Amount = reservation.TotalAmount,
            GatewayReference =
                "MOCK-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 10)
                    .ToUpper(),
            Status = "Paid"
        };

        reservation.Status = "Confirmed";
        reservation.ExpiresAt = null;

        _db.Payments.Add(payment);

        foreach (var reservationRoom in reservation.ReservationRooms)
        {
            var hold = await _db.RoomHolds
                .FirstOrDefaultAsync(h =>
                    h.RoomId == reservationRoom.RoomId &&
                    h.HeldByUserId == userId &&
                    h.DateFrom < reservation.CheckOutDate &&
                    h.DateTo > reservation.CheckInDate &&
                    h.HeldUntil > DateTime.UtcNow);

            if (hold != null)
            {
                _db.RoomHolds.Remove(hold);
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment successful. Reservation confirmed.",
            reservationId = reservation.Id,
            bookingReference = reservation.BookingReference,
            amount = payment.Amount,
            paymentStatus = payment.Status,
            reservationStatus = reservation.Status,
            gatewayReference = payment.GatewayReference
        });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook(
        int reservationId,
        bool paymentSuccessful)
    {
        var reservation = await _db.Reservations
            .Include(r => r.ReservationRooms)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != "PendingPayment")
        {
            return BadRequest(new
            {
                message = "Reservation is not awaiting payment."
            });
        }

        if (reservation.ExpiresAt.HasValue &&
            reservation.ExpiresAt.Value <= DateTime.UtcNow)
        {
            reservation.Status = "Expired";

            await _db.SaveChangesAsync();

            return BadRequest(new
            {
                message = "The payment window has expired."
            });
        }

        if (!paymentSuccessful)
        {
            var failedPayment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalAmount,
                GatewayReference =
                    "WEBHOOK-FAILED-" +
                    Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 10)
                        .ToUpper(),
                Status = "Failed"
            };

            _db.Payments.Add(failedPayment);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment failure webhook processed.",
                reservationId = reservation.Id,
                status = "PaymentFailed"
            });
        }

        var payment = new Payment
        {
            ReservationId = reservation.Id,
            Amount = reservation.TotalAmount,
            GatewayReference =
                "WEBHOOK-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 10)
                    .ToUpper(),
            Status = "Paid"
        };

        reservation.Status = "Confirmed";
        reservation.ExpiresAt = null;

        _db.Payments.Add(payment);

        foreach (var reservationRoom in reservation.ReservationRooms)
        {
            var hold = await _db.RoomHolds
                .FirstOrDefaultAsync(h =>
                    h.RoomId == reservationRoom.RoomId &&
                    h.DateFrom < reservation.CheckOutDate &&
                    h.DateTo > reservation.CheckInDate &&
                    h.HeldUntil > DateTime.UtcNow);

            if (hold != null)
            {
                _db.RoomHolds.Remove(hold);
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment webhook processed successfully.",
            reservationId = reservation.Id,
            paymentStatus = payment.Status,
            reservationStatus = reservation.Status,
            gatewayReference = payment.GatewayReference
        });
    }
}