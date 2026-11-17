namespace BookingService.DTOs;

public class CreateRazorpayOrderRequestDto
{
    public int TrainId { get; set; }

    public int ScheduleId { get; set; }

    public DateOnly JourneyDate { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public int PassengerCount { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;
}