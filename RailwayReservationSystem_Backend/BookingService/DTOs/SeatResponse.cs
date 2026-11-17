namespace BookingService.DTOs;

public class SeatResponse
{
    public int SeatId { get; set; }
    public int TrainId { get; set; }
    public int ScheduleId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string ClassType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}