namespace TrainService.Models;

public class RouteStation
{
    public int RouteStationId { get; set; }

    public int TrainId { get; set; }

    public string StationName { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public decimal DistanceFromOriginKm { get; set; }

    public Train Train { get; set; } = null!;
}