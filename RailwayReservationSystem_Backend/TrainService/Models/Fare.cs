namespace TrainService.Models;

public class Fare
{
    public int FareId { get; set; }

    public int TrainId { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Train Train { get; set; } = null!;
}