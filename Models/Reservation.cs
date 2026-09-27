namespace HotelBooking.Api.Models;

public class Reservation
{
    public int Id { get; set; }

    public int GuestId { get; set; }

    public int HotelId { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Held";

    public string BookingReference { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public User Guest { get; set; } = null!;

    public Hotel Hotel { get; set; } = null!;

    public ICollection<ReservationRoom> ReservationRooms { get; set; }
        = new List<ReservationRoom>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}