using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;

    public AvailabilityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CheckAvailability(
        AvailabilityRequest request)
    {
        if (request.CheckInDate.Date >= request.CheckOutDate.Date)
        {
            return BadRequest(new
            {
                message = "Check-out date must be after check-in date."
            });
        }

        if (request.CheckInDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new
            {
                message = "Check-in date cannot be in the past."
            });
        }

        var hotelExists = await _db.Hotels
            .AnyAsync(h => h.Id == request.HotelId);

        if (!hotelExists)
        {
            return NotFound(new
            {
                message = "Hotel not found."
            });
        }

        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .Where(r =>
                r.RoomType.HotelId == request.HotelId &&
                r.Status == "Active" &&
                r.RoomType.MaxOccupancy >= request.Guests)
            .ToListAsync();

        var now = DateTime.UtcNow;

        // Confirmed reservations always block the room.
        // PendingPayment reservations block the room
        // only while their payment window is still active.
        var unavailableRoomIds = await _db.ReservationRooms
            .Where(rr =>
                (
                    rr.Reservation.Status == "Confirmed"
                    ||
                    (
                        rr.Reservation.Status == "PendingPayment" &&
                        rr.Reservation.ExpiresAt > now
                    )
                )
                &&
                rr.Reservation.CheckInDate < request.CheckOutDate.Date &&
                rr.Reservation.CheckOutDate > request.CheckInDate.Date
            )
            .Select(rr => rr.RoomId)
            .Distinct()
            .ToListAsync();

        var heldRoomIds = await _db.RoomHolds
            .Where(h =>
                h.HeldUntil > now &&
                h.DateFrom < request.CheckOutDate.Date &&
                h.DateTo > request.CheckInDate.Date)
            .Select(h => h.RoomId)
            .Distinct()
            .ToListAsync();

        var availableRooms = rooms
            .Where(r =>
                !unavailableRoomIds.Contains(r.Id) &&
                !heldRoomIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.RoomNumber,
                RoomTypeId = r.RoomType.Id,
                RoomType = r.RoomType.TypeName,
                BaseRate = r.RoomType.BaseRate,
                r.RoomType.MaxOccupancy
            })
            .ToList();

        return Ok(new
        {
            hotelId = request.HotelId,
            checkInDate = request.CheckInDate.Date,
            checkOutDate = request.CheckOutDate.Date,
            guests = request.Guests,
            availableRooms
        });
    }
}