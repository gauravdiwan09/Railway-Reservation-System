namespace TrainService.DTOs.FareDTO;

public class FareRequestDto
{
    public string ClassType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}