namespace TrainService.Models;

public class SeatReservation
{
    public int SeatReservationId { get; set; }

    public int SeatId { get; set; }

    public DateOnly JourneyDate { get; set; }

    public decimal StartDistanceKm { get; set; }

    public decimal EndDistanceKm { get; set; }
}