namespace HotelBooking.Api.Models;

public class Room
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public RoomType RoomType { get; set; } = null!;

    public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();

    public ICollection<RoomHold> RoomHolds { get; set; } = new List<RoomHold>();
}