namespace BookingService.DTOs;

public class PassengerResponseDto
{
    public int PassengerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    public int SeatId { get; set; }
}