namespace BookingService.DTOs;

public class BookingResponseDto
{
    public int BookingId { get; set; }

    public string PNR { get; set; } = string.Empty;

    public int UserId { get; set; }

    public int TrainId { get; set; }

    public int ScheduleId { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public DateOnly JourneyDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int NumberOfTickets { get; set; }

    public decimal TotalFare { get; set; }

    public List<PassengerResponseDto> Passengers { get; set; }
        = new List<PassengerResponseDto>();
}