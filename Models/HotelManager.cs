namespace HotelBooking.Api.Models;

public class HotelManager
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ApprovalStatus { get; set; } = "Pending";

    public User User { get; set; } = null!;

    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}