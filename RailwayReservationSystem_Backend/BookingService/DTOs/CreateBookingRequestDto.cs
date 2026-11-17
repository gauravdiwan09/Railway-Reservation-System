namespace BookingService.DTOs;

public class CreateBookingRequestDto
{
    public int TrainId { get; set; }

    public int ScheduleId { get; set; }

    public DateOnly JourneyDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal StartDistanceKm { get; set; }

    public decimal EndDistanceKm { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public string PaymentId { get; set; } = string.Empty;

    public List<PassengerRequestDto> Passengers { get; set; }
        = new List<PassengerRequestDto>();
}