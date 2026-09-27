using System.Security.Claims;
using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Hotel Manager")]
public class PayoutsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PayoutsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayouts()
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

        var manager = await _db.HotelManagers
            .Include(hm => hm.Hotels)
            .FirstOrDefaultAsync(hm =>
                hm.UserId == userId);

        if (manager == null)
        {
            return NotFound(new
            {
                message = "Hotel manager assignment not found."
            });
        }

        var hotelIds = manager.Hotels
            .Select(h => h.Id)
            .ToList();

        if (!hotelIds.Any())
        {
            return Ok(new List<object>());
        }

        var payouts = await _db.Payments
            .AsNoTracking()
            .Where(p =>
                p.Status == "Paid" &&
                p.Reservation.Status == "Confirmed" &&
                hotelIds.Contains(
                    p.Reservation.HotelId))
            .Include(p => p.Reservation)
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                payoutId = p.Id,

                reservationId =
                    p.ReservationId,

                bookingReference =
                    p.Reservation.BookingReference,

                amount =
                    p.Amount,

                status = "Paid",

                createdAt =
                    p.CreatedAt,

                paidAt =
                    p.CreatedAt
            })
            .ToListAsync();

        return Ok(payouts);
    }
}