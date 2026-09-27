using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}