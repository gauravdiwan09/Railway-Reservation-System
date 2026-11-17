namespace BookingService.Services.RazorpayClientService;

public interface IRazorpayClient
{
    string KeyId { get; }

    bool VerifySignature(string orderId, string paymentId, string signature);

    Task<RazorpayOrderResult> CreateOrderAsync(long amount, string receipt, object notes);

    Task<RazorpayPaymentDetails> GetPaymentAsync(string paymentId);

    Task<RazorpayPaymentDetails> CapturePaymentAsync(string paymentId, long amount, string currency);

    Task RefundPaymentAsync(string paymentId, long amount);
}

public sealed record RazorpayOrderResult(string OrderId, long Amount, string Currency);

public sealed record RazorpayPaymentDetails(string PaymentId, string OrderId, long Amount, string Currency, string Status);