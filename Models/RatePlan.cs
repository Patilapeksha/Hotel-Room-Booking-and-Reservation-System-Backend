namespace HotelBooking.Api.Models;

public class RatePlan
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public decimal RateOverride { get; set; }

    public string RuleType { get; set; } = string.Empty;

    public RoomType RoomType { get; set; } = null!;
}