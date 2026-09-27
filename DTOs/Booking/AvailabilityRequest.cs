using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Booking;

public class AvailabilityRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 10)]
    public int Guests { get; set; } = 1;
}