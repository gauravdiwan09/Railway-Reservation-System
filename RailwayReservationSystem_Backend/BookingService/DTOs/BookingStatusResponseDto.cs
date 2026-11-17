namespace BookingService.DTOs;

public class BookingStatusResponseDto
{
    public string PNR { get; set; } = string.Empty;
    public DateOnly JourneyDate { get; set; }
    public string Status { get; set; } = string.Empty;
}