using System.Security.Claims;
using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Hotel Manager")]
public class ManagerReservationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ManagerReservationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPut("{id}/check-in")]
    public async Task<IActionResult> CheckIn(int id)
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
            .FirstOrDefaultAsync(hm => hm.UserId == userId);

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

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                hotelIds.Contains(r.HotelId));

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != "Confirmed")
        {
            return BadRequest(new
            {
                message =
                    "Only confirmed reservations can be checked in."
            });
        }

        reservation.Status = "CheckedIn";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Guest checked in successfully.",
            reservationId = reservation.Id,
            status = reservation.Status
        });
    }

    [HttpPut("{id}/check-out")]
    public async Task<IActionResult> CheckOut(int id)
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
            .FirstOrDefaultAsync(hm => hm.UserId == userId);

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

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                hotelIds.Contains(r.HotelId));

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Reservation not found."
            });
        }

        if (reservation.Status != "CheckedIn")
        {
            return BadRequest(new
            {
                message =
                    "Only checked-in reservations can be checked out."
            });
        }

        reservation.Status = "CheckedOut";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Guest checked out successfully.",
            reservationId = reservation.Id,
            status = reservation.Status
        });
    }
}