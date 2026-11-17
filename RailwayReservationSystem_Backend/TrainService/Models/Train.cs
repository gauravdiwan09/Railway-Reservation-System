namespace TrainService.Models;

public class Train
{
    public int TrainId { get; set; }

    public string TrainNumber { get; set; } = string.Empty;

    public string TrainName { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public decimal TotalDistanceKm { get; set; }

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public ICollection<Fare> Fares { get; set; } = new List<Fare>();

    public ICollection<RouteStation> RouteStations { get; set; } = new List<RouteStation>();
}