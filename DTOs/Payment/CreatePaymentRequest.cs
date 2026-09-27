using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Api.DTOs.Payment;

public class CreatePaymentRequest
{
    [Required]
    public int ReservationId { get; set; }

    [Required]
    public bool PaymentSuccessful { get; set; }
}