namespace BookingService.DTOs;

public class PassengerRequestDto
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Gender { get; set; } = string.Empty;
}