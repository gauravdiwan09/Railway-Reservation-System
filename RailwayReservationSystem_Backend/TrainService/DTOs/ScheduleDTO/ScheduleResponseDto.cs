namespace TrainService.DTOs.ScheduleDTO;

public class ScheduleResponseDto
{
    public int ScheduleId { get; set; }
    public int TrainId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public bool ArrivalNextDay {get; set;}
    public string DaysOfOperation { get; set; } = string.Empty;
}