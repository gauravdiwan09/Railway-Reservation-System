namespace TrainService.Models;

public class Seat
{
    public int SeatId { get; set; }

    public int TrainId { get; set; }

    public int ScheduleId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string ClassType { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public decimal? StartDistanceKm { get; set; }

    public decimal? EndDistanceKm { get; set; }

    public Train Train { get; set; } = null!;

    public Schedule Schedule { get; set; } = null!;
}