namespace BookingService.DTOs;

public class BookingCancellationResponseDto
{
    public int BookingId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string RefundStatus { get; set; } = string.Empty;
}