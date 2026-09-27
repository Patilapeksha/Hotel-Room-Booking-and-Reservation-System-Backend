using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Pricing;

public class CalculatePriceRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }
}