namespace TrainService.DTOs.FareDTO;

public class FareResponseDto
{
    public int FareId { get; set; }
    public int TrainId { get; set; }
    public string ClassType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}