namespace HotelBooking.Api.Models;

public class Payout
{
    public int Id { get; set; }

    public int ManagerId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime? ProcessedAt { get; set; }

    public HotelManager Manager { get; set; } = null!;
}