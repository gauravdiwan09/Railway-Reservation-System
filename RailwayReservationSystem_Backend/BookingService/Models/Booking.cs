namespace BookingService.Models;

public class Booking
{
    public int BookingId { get; set; }

    public string PNR { get; set; } = string.Empty;

    public string PaymentId { get; set; } = string.Empty;

    // Reference ID from AuthService
    public int UserId { get; set; }

    // Reference ID from TrainService
    public int TrainId { get; set; }

    // Reference ID from TrainService
    public int ScheduleId { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public DateOnly JourneyDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal StartDistanceKm { get; set; }

    public decimal EndDistanceKm { get; set; }

    public string Status { get; set; } = string.Empty;

    public int NumberOfTickets { get; set; }

    public decimal TotalFare { get; set; }

    public ICollection<Passenger> Passengers { get; set; }
        = new List<Passenger>();
}