using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Booking;

public class CreateHoldRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }
}