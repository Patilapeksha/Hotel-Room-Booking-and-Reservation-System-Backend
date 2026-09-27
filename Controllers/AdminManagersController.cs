using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminManagersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminManagersController(AppDbContext db)
    {
        _db = db;
    }

    // Get all hotel managers
    [HttpGet]
    public async Task<IActionResult> GetManagers()
    {
        var managers = await _db.HotelManagers
            .Include(m => m.User)
            .Include(m => m.Hotels)
            .Select(m => new
            {
                managerId = m.Id,
                userId = m.UserId,
                name = m.User.Name,
                email = m.User.Email,
                approvalStatus = m.ApprovalStatus,
                hotels = m.Hotels
                    .Select(h => new
                    {
                        id = h.Id,
                        name = h.Name,
                        city = h.City
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(managers);
    }

    // Approve manager
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveManager(int id)
    {
        var manager = await _db.HotelManagers
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manager == null)
        {
            return NotFound(new
            {
                message = "Hotel manager not found."
            });
        }

        manager.ApprovalStatus = "Approved";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Hotel manager approved successfully.",
            managerId = manager.Id,
            approvalStatus = manager.ApprovalStatus
        });
    }

    // Reject manager
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectManager(int id)
    {
        var manager = await _db.HotelManagers
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manager == null)
        {
            return NotFound(new
            {
                message = "Hotel manager not found."
            });
        }

        manager.ApprovalStatus = "Rejected";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Hotel manager rejected.",
            managerId = manager.Id,
            approvalStatus = manager.ApprovalStatus
        });
    }
}