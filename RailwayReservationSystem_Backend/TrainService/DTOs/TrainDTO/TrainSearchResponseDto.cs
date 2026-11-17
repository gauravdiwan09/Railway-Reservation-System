namespace TrainService.DTOs.TrainDTO;

public class TrainSearchResponseDto
{
    public int TrainId { get; set; }
    public string TrainNumber { get; set; } = string.Empty;
    public string TrainName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int TotalSeats { get; set; }

    public int ScheduleId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public bool ArrivalNextDay {get; set;}

    public decimal StartDistanceKm { get; set; }

    public decimal EndDistanceKm { get; set; }

    public decimal TotalDistanceKm { get; set; }
}