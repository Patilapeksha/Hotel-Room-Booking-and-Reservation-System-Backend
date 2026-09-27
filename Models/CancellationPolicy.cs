namespace HotelBooking.Api.Models;

public class CancellationPolicy
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public int DaysBeforeCheckIn { get; set; }

    public decimal RefundPercentage { get; set; }

    public Hotel Hotel { get; set; } = null!;
}