using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainService.DTOs.FareDTO;
using TrainService.Services.FareService;

namespace TrainService.Controllers;

[ApiController]
[Authorize]
public class FaresController : ControllerBase
{
    private readonly IFareService _fareService;

    public FaresController(IFareService fareService)
    {
        _fareService = fareService;
    }

    // GET: /api/trains/{trainId}/fare
    [HttpGet("api/trains/{trainId}/fare")]
    public async Task<IActionResult> GetByTrainId(int trainId)
    {
        var fares = await _fareService.GetByTrainIdAsync(trainId);

        return Ok(fares);
    }

    // GET: /api/fares/{id}
    [HttpGet("api/fares/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var fare = await _fareService.GetByIdAsync(id);

        return Ok(fare);
    }

    // POST: /api/trains/{trainId}/fare
    [HttpPost("api/trains/{trainId}/fare")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(
        int trainId,
        FareRequestDto request)
    {
            var fare = await _fareService.AddAsync(trainId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = fare.FareId },
                fare);
    }

    // PUT: /api/fares/{id}
    [HttpPut("api/fares/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        FareRequestDto request)
    {
            var fare = await _fareService.UpdateAsync(id, request);

            return Ok(fare);
    }

    // DELETE: /api/fares/{id}
    [HttpDelete("api/fares/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _fareService.DeleteAsync(id);

        return NoContent();
    }
}