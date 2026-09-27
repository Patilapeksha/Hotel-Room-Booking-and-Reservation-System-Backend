using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminPayoutsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminPayoutsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayouts()
    {
        var payouts = await _db.Payments
            .AsNoTracking()
            .Where(p =>
                p.Status == "Paid" &&
                p.Reservation.Status == "Confirmed")
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Hotel)
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Guest)
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                payoutId = p.Id,

                bookingReference =
                    p.Reservation.BookingReference,

                guestName =
                    p.Reservation.Guest.Name,

                hotelName =
                    p.Reservation.Hotel.Name,

                amount =
                    p.Amount,

                paymentStatus =
                    p.Status,

                gatewayReference =
                    p.GatewayReference,

                createdAt =
                    p.CreatedAt
            })
            .ToListAsync();

        return Ok(payouts);
    }
}