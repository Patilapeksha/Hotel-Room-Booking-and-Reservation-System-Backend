using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new { message = "Public endpoint is working." });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You are authorized as Admin." });
    }

    [HttpGet("manager")]
    [Authorize(Roles = "Hotel Manager")]
    public IActionResult ManagerOnly()
    {
        return Ok(new { message = "You are authorized as Hotel Manager." });
    }

    [HttpGet("guest")]
    [Authorize(Roles = "Guest")]
    public IActionResult GuestOnly()
    {
        return Ok(new { message = "You are authorized as Guest." });
    }
}