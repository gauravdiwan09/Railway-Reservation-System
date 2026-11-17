namespace TrainService.DTOs.ScheduleDTO;

public class ScheduleRequestDto
{
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public bool ArrivalNextDay {get; set;}
    public string DaysOfOperation { get; set; } = string.Empty;
}