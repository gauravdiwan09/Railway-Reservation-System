namespace BookingService.Models;

public class Passenger
{
    public int PassengerId { get; set; }

    // FK within BookingDB
    public int BookingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    // Reference ID from TrainService
    public int SeatId { get; set; }

    public Booking Booking { get; set; } = null!;
}