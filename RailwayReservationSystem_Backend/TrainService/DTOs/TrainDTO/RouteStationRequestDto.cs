using System.ComponentModel.DataAnnotations;

namespace TrainService.DTOs.TrainDTO;

public class RouteStationRequestDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Station name must only contain letters and spaces.")]
    public string StationName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DistanceFromOriginKm { get; set; }
}