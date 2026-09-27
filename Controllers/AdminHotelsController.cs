using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminHotelsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminHotelsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetHotels()
    {
        var hotels = await _db.Hotels
            .AsNoTracking()
            .Select(h => new
            {
                id = h.Id,
                name = h.Name,
                city = h.City,
                address = h.Address,
                status = h.Status
            })
            .ToListAsync();

        return Ok(hotels);
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateHotel(int id)
    {
        var hotel = await _db.Hotels
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel == null)
        {
            return NotFound(new
            {
                message = "Hotel not found."
            });
        }

        hotel.Status = "Active";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Hotel activated successfully.",
            hotelId = hotel.Id,
            status = hotel.Status
        });
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateHotel(int id)
    {
        var hotel = await _db.Hotels
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel == null)
        {
            return NotFound(new
            {
                message = "Hotel not found."
            });
        }

        hotel.Status = "Inactive";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Hotel deactivated successfully.",
            hotelId = hotel.Id,
            status = hotel.Status
        });
    }
}