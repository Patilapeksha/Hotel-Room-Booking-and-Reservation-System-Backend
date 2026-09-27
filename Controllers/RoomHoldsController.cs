using System.Data;
using System.Security.Claims;
using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Booking;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Guest")]
public class RoomHoldsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomHoldsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHold(
        CreateHoldRequest request)
    {
        // Validate dates
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

        // Get logged-in user
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        // Start SQL Server transaction
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // Lock the room row.
            // This prevents two simultaneous hold requests
            // from processing the same room at the same time.
            var roomLock = await _db.Database
                .SqlQueryRaw<int>(
                    "SELECT Id AS Value FROM Rooms WITH (UPDLOCK, HOLDLOCK) WHERE Id = {0}",
                    request.RoomId)
                .FirstOrDefaultAsync();

            if (roomLock == 0)
            {
                await transaction.RollbackAsync();

                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            // Get room details
            var room = await _db.Rooms
                .Include(r => r.RoomType)
                .ThenInclude(rt => rt.Hotel)
                .FirstOrDefaultAsync(r =>
                    r.Id == request.RoomId &&
                    r.Status == "Active");

            if (room == null)
            {
                await transaction.RollbackAsync();

                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            // Check existing confirmed/pending reservations
            var existingReservation =
                await _db.ReservationRooms
                    .AnyAsync(rr =>
                        rr.RoomId == request.RoomId &&
                        rr.Reservation.Status != "Cancelled" &&
                        rr.Reservation.Status != "Expired" &&
                        rr.Reservation.CheckInDate < request.CheckOutDate.Date &&
                        rr.Reservation.CheckOutDate > request.CheckInDate.Date);

            if (existingReservation)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "Room is already booked for these dates."
                });
            }

            // Remove expired holds for this room
            var expiredHolds = await _db.RoomHolds
                .Where(h =>
                    h.RoomId == request.RoomId &&
                    h.HeldUntil <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredHolds.Any())
            {
                _db.RoomHolds.RemoveRange(expiredHolds);

                await _db.SaveChangesAsync();
            }

            // Check active overlapping hold
            var existingHold = await _db.RoomHolds
                .AnyAsync(h =>
                    h.RoomId == request.RoomId &&
                    h.HeldUntil > DateTime.UtcNow &&
                    h.DateFrom < request.CheckOutDate.Date &&
                    h.DateTo > request.CheckInDate.Date);

            if (existingHold)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "Room is currently held by another booking."
                });
            }

            // Create new hold
            var hold = new RoomHold
            {
                RoomId = request.RoomId,
                DateFrom = request.CheckInDate.Date,
                DateTo = request.CheckOutDate.Date,
                HeldByUserId = userId,
                HeldUntil = DateTime.UtcNow.AddMinutes(10)
            };

            _db.RoomHolds.Add(hold);

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Room held successfully.",

                holdId = hold.Id,

                roomId = hold.RoomId,

                roomNumber = room.RoomNumber,

                hotel = room.RoomType.Hotel.Name,

                checkInDate = hold.DateFrom,

                checkOutDate = hold.DateTo,

                heldUntil = hold.HeldUntil,

                pricePerNight = room.RoomType.BaseRate
            });
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Unable to hold the room."
            });
        }
    }
}