namespace HotelBooking.Api.Models;

public class Hotel
{
    public int Id { get; set; }

    public int ManagerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int StarRating { get; set; }

    public string Amenities { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public HotelManager Manager { get; set; } = null!;

    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}