using HotelBooking.Api.Data;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Hotel Manager")]
public class RoomTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomTypesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{hotelId}")]
    public async Task<IActionResult> GetRoomTypes(int hotelId)
    {
        var roomTypes = await _db.RoomTypes
            .Where(r => r.HotelId == hotelId)
            .Select(r => new
            {
                id = r.Id,
                hotelId = r.HotelId,
                typeName = r.TypeName,
                baseRate = r.BaseRate,
                maxOccupancy = r.MaxOccupancy
            })
            .ToListAsync();

        return Ok(roomTypes);
    }

    [HttpPost]
    public async Task<IActionResult> AddRoomType(
        int hotelId,
        string typeName,
        decimal baseRate,
        int maxOccupancy)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return BadRequest(new
            {
                message = "Room type name is required."
            });
        }

        if (baseRate <= 0)
        {
            return BadRequest(new
            {
                message = "Base rate must be greater than zero."
            });
        }

        if (maxOccupancy <= 0)
        {
            return BadRequest(new
            {
                message = "Maximum occupancy must be greater than zero."
            });
        }

        var hotelExists = await _db.Hotels
            .AnyAsync(h => h.Id == hotelId);

        if (!hotelExists)
        {
            return NotFound(new
            {
                message = "Hotel not found."
            });
        }

        var exists = await _db.RoomTypes
            .AnyAsync(r =>
                r.HotelId == hotelId &&
                r.TypeName == typeName);

        if (exists)
        {
            return BadRequest(new
            {
                message = "Room type already exists."
            });
        }

        var roomType = new RoomType
        {
            HotelId = hotelId,
            TypeName = typeName.Trim(),
            BaseRate = baseRate,
            MaxOccupancy = maxOccupancy
        };

        _db.RoomTypes.Add(roomType);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Room type added successfully.",
            roomTypeId = roomType.Id
        });
    }
}