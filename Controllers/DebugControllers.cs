using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly AppDbContext _db;

    public DebugController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> CheckRoom(int roomId)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound(new
            {
                message = "Room not found."
            });
        }

        var now = DateTime.UtcNow;

        var reservations = await _db.ReservationRooms
            .Where(rr => rr.RoomId == roomId)
            .Select(rr => new
            {
                reservationId = rr.ReservationId,
                status = rr.Reservation.Status,
                checkInDate = rr.Reservation.CheckInDate,
                checkOutDate = rr.Reservation.CheckOutDate,
                expiresAt = rr.Reservation.ExpiresAt
            })
            .ToListAsync();

        var activeBookings = reservations
            .Where(r =>
                r.status == "Confirmed" ||
                (r.status == "PendingPayment" &&
                 r.expiresAt > now))
            .ToList();

        return Ok(new
        {
            roomId = room.Id,
            roomNumber = room.RoomNumber,
            allReservations = reservations,
            activeBookings = activeBookings
        });
    }
}