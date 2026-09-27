using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricingController : ControllerBase
{
    private readonly AppDbContext _db;

    public PricingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePrice(
        CalculatePriceRequest request)
    {
        if (request.CheckInDate.Date >= request.CheckOutDate.Date)
        {
            return BadRequest(new
            {
                message = "Check-out date must be after check-in date."
            });
        }

        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r =>
                r.Id == request.RoomId &&
                r.Status == "Active");

        if (room == null)
        {
            return NotFound(new
            {
                message = "Room not found."
            });
        }

        var nights =
            (request.CheckOutDate.Date - request.CheckInDate.Date).Days;

        var baseRate = room.RoomType.BaseRate;

        var ratePlans = await _db.RatePlans
            .Where(r =>
                r.RoomTypeId == room.RoomTypeId &&
                r.DateFrom < request.CheckOutDate.Date &&
                r.DateTo > request.CheckInDate.Date)
            .OrderBy(r => r.DateFrom)
            .ToListAsync();

        decimal totalAmount = 0;

        for (var date = request.CheckInDate.Date;
             date < request.CheckOutDate.Date;
             date = date.AddDays(1))
        {
            var ratePlan = ratePlans
                .FirstOrDefault(r =>
                    r.DateFrom.Date <= date &&
                    r.DateTo.Date > date);

            var nightlyRate = ratePlan?.RateOverride ?? baseRate;

            totalAmount += nightlyRate;
        }

        return Ok(new
        {
            roomId = room.Id,
            roomNumber = room.RoomNumber,
            roomType = room.RoomType.TypeName,
            checkInDate = request.CheckInDate.Date,
            checkOutDate = request.CheckOutDate.Date,
            nights,
            baseRate,
            totalAmount
        });
    }
}