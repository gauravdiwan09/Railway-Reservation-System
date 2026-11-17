namespace TrainService.DTOs.SeatDTO;

public class SeatResponseDto
{
    public int SeatId { get; set; }
    public int TrainId { get; set; }
    public int ScheduleId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string ClassType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }

    public decimal? StartDistanceKm { get; set; }

    public decimal? EndDistanceKm { get; set; }
}