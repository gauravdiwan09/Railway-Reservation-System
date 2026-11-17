namespace BookingService.DTOs;

public class TrainRouteStationDto
{
    public string StationName { get; set; } = string.Empty;

    public decimal DistanceFromOriginKm { get; set; }
}