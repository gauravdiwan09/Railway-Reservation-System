namespace TrainService.Models;

public class SeatAvailability
{
    public int SeatAvailabilityId { get; set; }

    public int SeatId { get; set; }

    public DateOnly JourneyDate { get; set; }

    public bool IsAvailable { get; set; }

    public Seat Seat { get; set; } = null!;
}