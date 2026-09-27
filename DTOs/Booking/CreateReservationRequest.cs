using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Booking;

public class CreateReservationRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public int HoldId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }
}