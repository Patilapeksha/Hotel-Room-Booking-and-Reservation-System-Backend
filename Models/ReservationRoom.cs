namespace HotelBooking.Api.Models;

public class ReservationRoom
{
    public int ReservationId { get; set; }

    public int RoomId { get; set; }

    public Reservation Reservation { get; set; } = null!;

    public Room Room { get; set; } = null!;
}