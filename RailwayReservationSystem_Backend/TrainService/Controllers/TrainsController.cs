using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainService.DTOs.TrainDTO;
using TrainService.Services.TrainService;

namespace TrainService.Controllers;

[ApiController]
[Route("api/trains")]
[Authorize]
public class TrainsController : ControllerBase
{
    private readonly ITrainService _trainService;

    public TrainsController(ITrainService trainService)
    {
        _trainService = trainService;
    }

    // GET: api/trains
    [HttpGet]
    public async Task<ActionResult<List<TrainResponseDto>>> GetAll()
    {
        var trains = await _trainService.GetAllAsync();

        return Ok(trains);
    }

    // GET: api/trains/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<TrainResponseDto>> GetById(int id)
    {
        var train = await _trainService.GetByIdAsync(id);

        return Ok(train);
    }

    // POST: api/trains
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TrainResponseDto>> Add(
        TrainRequestDto request)
    {
        if (string.Equals(request.Source.Trim(), request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Source and destination locations cannot be the same." });
        }

        var validationMessage = ValidateRoute(request);
        if (validationMessage != null) return BadRequest(new { message = validationMessage });

            var train = await _trainService.AddAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = train.TrainId },
                train);
    }

    // PUT: api/trains/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TrainResponseDto>> Update(
        int id,
        TrainRequestDto request)
    {
        var validationMessage = ValidateRoute(request);
        if (validationMessage != null) return BadRequest(new { message = validationMessage });

            var train = await _trainService.UpdateAsync(id, request);

            return Ok(train);
    }

    // DELETE: api/trains/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _trainService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string source,
        [FromQuery] string destination,
        [FromQuery] DateTime date)
    {
        if (string.Equals(source?.Trim(), destination?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Source and destination locations cannot be the same." });
        }

        var trains = await _trainService.SearchAsync(
            source ?? "",
            destination ?? "",
            date);

        return Ok(trains);
    }

    private static string? ValidateRoute(TrainRequestDto request)
    {
        var stops = request.IntermediateStations;
        var previousDistance = 0m;
        var stationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            request.Source.Trim(), request.Destination.Trim()
        };

        foreach (var stop in stops)
        {
            if (!stationNames.Add(stop.StationName.Trim())) return "Route station names must be unique.";
            if (stop.DistanceFromOriginKm <= previousDistance) return "Intermediate station distances must increase along the route.";
            previousDistance = stop.DistanceFromOriginKm;
        }

        if (request.TotalDistanceKm > 0 && request.TotalDistanceKm <= previousDistance)
            return "Total route distance must be greater than every intermediate station distance.";

        return null;
    }
}