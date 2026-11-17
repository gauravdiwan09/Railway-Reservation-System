namespace BookingService.DTOs;

public class RazorpayPaymentResponseDto
{
    public string PaymentId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}