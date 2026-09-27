using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HotelsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHotels()
    {
        var hotels = await _db.Hotels
            .Include(h => h.RoomTypes)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Description,
                h.City,
                h.Address,
                h.StarRating,
                h.Amenities,
                RoomTypes = h.RoomTypes.Select(r => new
                {
                    r.Id,
                    r.TypeName,
                    r.BaseRate,
                    r.MaxOccupancy
                })
            })
            .ToListAsync();

        return Ok(hotels);
    }
}