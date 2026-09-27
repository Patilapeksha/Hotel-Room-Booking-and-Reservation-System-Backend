using HotelBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        var totalHotels = await _db.Hotels.CountAsync();

        var activeHotels = await _db.Hotels
            .CountAsync(h => h.Status == "Active");

        var totalManagers = await _db.HotelManagers.CountAsync();

        var totalBookings = await _db.Reservations.CountAsync();

        var confirmedBookings = await _db.Reservations
            .CountAsync(r => r.Status == "Confirmed");

        var pendingPayments = await _db.Reservations
            .CountAsync(r => r.Status == "PendingPayment");

        var paidPayments = await _db.Payments
            .CountAsync(p => p.Status == "Paid");

        var totalRevenue = await _db.Payments
            .Where(p => p.Status == "Paid")
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return Ok(new
        {
            totalHotels,
            activeHotels,
            totalManagers,
            totalBookings,
            confirmedBookings,
            pendingPayments,
            paidPayments,
            totalRevenue
        });
    }
}