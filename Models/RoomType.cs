namespace HotelBooking.Api.Models;

public class RoomType
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public decimal BaseRate { get; set; }

    public int MaxOccupancy { get; set; }

    public Hotel Hotel { get; set; } = null!;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();

    public ICollection<RatePlan> RatePlans { get; set; } = new List<RatePlan>();
}