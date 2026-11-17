namespace BookingService.DTOs;

public class VerifyRazorpayPaymentRequestDto
{
    public string PaymentId { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string RazorpayPaymentId { get; set; } = string.Empty;

    public string RazorpaySignature { get; set; } = string.Empty;
}