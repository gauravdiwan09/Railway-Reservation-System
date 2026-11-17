using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainService.DTOs.SeatDTO;
using TrainService.Services.SeatService;

namespace TrainService.Controllers;

[ApiController]
[Authorize]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatsController(ISeatService seatService)
    {
        _seatService = seatService;
    }

    // GET /api/trains/{trainId}/seats?scheduleId=1&journeyDate=2026-09-16
    [HttpGet("api/trains/{trainId}/seats")]
    // public async Task<IActionResult> GetByTrainAndSchedule(
    //     int trainId,
    //     int scheduleId)
    // {
    //     var seats = await _seatService
    //         .GetByTrainAndScheduleAsync(trainId, scheduleId);

    //     return Ok(seats);
    // }
    public async Task<IActionResult> GetByTrainAndSchedule(
        int trainId,
        int scheduleId,
        DateOnly journeyDate,
        decimal? startDistanceKm,
        decimal? endDistanceKm)
    {
        var seats = await _seatService
            .GetByTrainAndScheduleAsync(trainId, scheduleId, journeyDate, startDistanceKm, endDistanceKm);

        return Ok(seats);
    }

    // GET: /api/seats/{id}
    [HttpGet("api/seats/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var seat = await _seatService.GetByIdAsync(id);

        return Ok(seat);
    }

    // POST: /api/trains/{trainId}/schedules/{scheduleId}/seats
    [HttpPost("api/trains/{trainId}/schedules/{scheduleId}/seats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(
        int trainId,
        int scheduleId,
        SeatRequestDto request)
    {
            var seat = await _seatService.AddAsync(
                trainId,
                scheduleId,
                request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = seat.SeatId },
                seat);
    }

    // PUT: /api/seats/{id}
    [HttpPut("api/seats/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        SeatRequestDto request)
    {
            var seat = await _seatService.UpdateAsync(id, request);

            return Ok(seat);
    }

    // DELETE: /api/seats/{id}
    [HttpDelete("api/seats/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _seatService.DeleteAsync(id);

        return NoContent();
    }

    // INTERNAL: Booking Service
    // PUT /api/trains/seats/{seatId}/reserve?journeyDate=2026-09-16
    [HttpPut("api/trains/seats/{seatId}/reserve")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Reserve(int seatId, DateOnly journeyDate, decimal? startDistanceKm, decimal? endDistanceKm)
    {
        var reserved = await _seatService.ReserveAsync(seatId, journeyDate, startDistanceKm, endDistanceKm);

        if (!reserved)
        {
            return Conflict(new
            {
                message = "Seat does not exist or is already reserved."
            });
        }

        return Ok(new
        {
            message = "Seat reserved successfully."
        });
    }

    // INTERNAL: Booking Service
    // PUT /api/trains/seats/{seatId}/release?journeyDate=2026-09-16
    [HttpPut("api/trains/seats/{seatId}/release")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Release(int seatId, DateOnly journeyDate, decimal? startDistanceKm, decimal? endDistanceKm)
    {
        var released = await _seatService.ReleaseAsync(seatId, journeyDate, startDistanceKm, endDistanceKm);

        if (!released)
        {
            return NotFound(new
            {
                message = "Seat not found."
            });
        }

        return Ok(new
        {
            message = "Seat released successfully."
        });
    }
}