using HotelBooking.Api.Data;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Hotel Manager")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{hotelId}")]
    public async Task<IActionResult> GetRooms(int hotelId)
    {
        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.RoomType.HotelId == hotelId)
            .Select(r => new
            {
                id = r.Id,
                roomNumber = r.RoomNumber,
                roomType = r.RoomType.TypeName,
                baseRate = r.RoomType.BaseRate,
                maxOccupancy = r.RoomType.MaxOccupancy,
                status = r.Status
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<IActionResult> AddRoom(
        int hotelId,
        string roomNumber,
        int roomTypeId,
        string status = "Active")
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(r =>
                r.Id == roomTypeId &&
                r.HotelId == hotelId);

        if (roomType == null)
        {
            return NotFound(new
            {
                message = "Room type not found."
            });
        }

        var exists = await _db.Rooms
            .AnyAsync(r =>
                r.RoomNumber == roomNumber &&
                r.RoomType.HotelId == hotelId);

        if (exists)
        {
            return BadRequest(new
            {
                message = "Room number already exists."
            });
        }

        var room = new Room
        {
            RoomNumber = roomNumber,
            RoomTypeId = roomTypeId,
            Status = status
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Room added successfully.",
            roomId = room.Id
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoom(
        int id,
        string roomNumber,
        int roomTypeId,
        string status)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            return NotFound(new
            {
                message = "Room not found."
            });
        }

        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(r =>
                r.Id == roomTypeId &&
                r.HotelId == room.RoomType.HotelId);

        if (roomType == null)
        {
            return NotFound(new
            {
                message = "Room type not found."
            });
        }

        room.RoomNumber = roomNumber;
        room.RoomTypeId = roomTypeId;
        room.Status = status;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Room updated successfully."
        });
    }


    [HttpGet("count")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetRoomCounts()
{
    var roomTypes = await _db.RoomTypes.CountAsync();
    var rooms = await _db.Rooms.CountAsync();

    return Ok(new
    {
        roomTypes,
        rooms
    });
}
}