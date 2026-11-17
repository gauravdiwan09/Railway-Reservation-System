namespace TrainService.DTOs.SeatDTO;

public class SeatRequestDto
{
    public string SeatNumber { get; set; } = string.Empty;
    public string ClassType { get; set; } = string.Empty;

    public decimal? StartDistanceKm { get; set; }

    public decimal? EndDistanceKm { get; set; }
}