namespace TrainService.DTOs.TrainDTO;

public class TrainResponseDto
{
    public int TrainId { get; set; }

    public string TrainNumber { get; set; } = string.Empty;

    public string TrainName { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public decimal TotalDistanceKm { get; set; }

    public List<RouteStationRequestDto> IntermediateStations { get; set; } = new();
}