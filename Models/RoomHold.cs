namespace HotelBooking.Api.Models;

public class RoomHold
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public int HeldByUserId { get; set; }

    public DateTime HeldUntil { get; set; }

    public Room Room { get; set; } = null!;

    public User HeldByUser { get; set; } = null!;
}