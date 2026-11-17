namespace TrainService.Models;

public class Schedule
{
    public int ScheduleId { get; set; }

    public int TrainId { get; set; }

    public TimeSpan DepartureTime { get; set; }

    public TimeSpan ArrivalTime { get; set; }

    public bool ArrivalNextDay {get; set;}

    public string DaysOfOperation { get; set; } = string.Empty;

    public Train Train { get; set; } = null!;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}