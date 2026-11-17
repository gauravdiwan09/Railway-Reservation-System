namespace BookingService.Models;

public class Payment
{
    public string PaymentId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public int TrainId { get; set; }

    public int ScheduleId { get; set; }

    public DateOnly JourneyDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string ClassType { get; set; } = string.Empty;

    public int PassengerCount { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = "Razorpay";

    public string ProviderReference { get; set; } = string.Empty;

    public string? RazorpayPaymentId { get; set; }

    public string Status { get; set; } = "OrderCreated";

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }
}