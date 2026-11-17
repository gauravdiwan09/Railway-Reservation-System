using System.ComponentModel.DataAnnotations;

namespace TrainService.DTOs.TrainDTO;

public class TrainRequestDto
{
    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Train number must be exactly a 5-digit number.")]
    public string TrainNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Train name must only contain letters and spaces.")]
    public string TrainName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Source must only contain letters and spaces.")]
    public string Source { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Destination must only contain letters and spaces.")]
    public string Destination { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Total seats must be at least 1.")]
    public int TotalSeats { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalDistanceKm { get; set; }

    public List<RouteStationRequestDto> IntermediateStations { get; set; } = new();
}
