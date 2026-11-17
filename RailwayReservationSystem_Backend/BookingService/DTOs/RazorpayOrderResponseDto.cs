namespace BookingService.DTOs;

public class RazorpayOrderResponseDto
{
    public string PaymentId { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public long Amount { get; set; }

    public string Currency { get; set; } = "INR";
}