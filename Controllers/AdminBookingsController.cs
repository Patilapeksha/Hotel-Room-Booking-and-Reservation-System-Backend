using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBookingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetBookings()
    {
        var bookings = await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Hotel)
            .Include(r => r.ReservationRooms)
                .ThenInclude(rr => rr.Room)
            .Include(r => r.Payments)
            .OrderByDescending(r => r.Id)
            .Select(r => new
            {
                id = r.Id,
                bookingReference = r.BookingReference,

                guestName = r.Guest.Name,
                guestEmail = r.Guest.Email,

                hotelName = r.Hotel.Name,
                hotelCity = r.Hotel.City,

                checkInDate = r.CheckInDate,
                checkOutDate = r.CheckOutDate,

                totalAmount = r.TotalAmount,
                status = r.Status,

                paymentStatus = r.Payments
                    .OrderByDescending(p => p.Id)
                    .Select(p => p.Status)
                    .FirstOrDefault(),

                rooms = r.ReservationRooms
                    .Select(rr => new
                    {
                        roomId = rr.RoomId,
                        roomNumber = rr.Room.RoomNumber
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(bookings);
    }
}