using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainService.DTOs.ScheduleDTO;
using TrainService.Repositories.ScheduleRepository;
using TrainService.Services.ScheduleService;

namespace TrainService.Controllers;

[ApiController]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IScheduleRepository _scheduleRepository;

    public SchedulesController(IScheduleService scheduleService, IScheduleRepository scheduleRepository)
    {
        _scheduleService = scheduleService;
        _scheduleRepository = scheduleRepository;
    }

    // GET: /api/trains/{trainId}/schedules
    [HttpGet("api/trains/{trainId}/schedules")]
    public async Task<IActionResult> GetByTrainId(int trainId)
    {
        var schedules = await _scheduleService.GetByTrainIdAsync(trainId);

        return Ok(schedules);
    }

    // GET: /api/schedules/{id}
    [HttpGet("api/schedules/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _scheduleService.GetByIdAsync(id);

        return Ok(schedule);
    }

    // POST: /api/trains/{trainId}/schedules
    [HttpPost("api/trains/{trainId}/schedules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(
        int trainId,
        ScheduleRequestDto request)
    {
            var schedule = await _scheduleService.AddAsync(trainId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = schedule.ScheduleId },
                schedule);
    }

    // PUT: /api/schedules/{id}
    [HttpPut("api/schedules/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        ScheduleRequestDto request)
    {
            var schedule = await _scheduleService.UpdateAsync(id, request);

            return Ok(schedule);
    }

    // DELETE: /api/schedules/{id}
    [HttpDelete("api/schedules/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _scheduleService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("api/trains/{trainId}/schedules/{scheduleId}/availability")]
    public async Task<ActionResult<bool>> CheckScheduleAvailability(
        int trainId,
        int scheduleId,
        [FromQuery] DateOnly journeyDate)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);

        if (schedule == null || schedule.TrainId != trainId)
            return NotFound();

        var requestedDay = journeyDate.DayOfWeek
            .ToString()
            .Substring(0, 3);

        Console.WriteLine($"Journey Date: {journeyDate}");
        Console.WriteLine($"Day of Week: {journeyDate.DayOfWeek}");
        Console.WriteLine($"Requested Day: {requestedDay}");
        Console.WriteLine($"Day of Operation: {schedule.DaysOfOperation}");

        var isOperating = schedule.DaysOfOperation
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(day => day.Trim())
            .Any(day => string.Equals(day, requestedDay, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine(schedule.DaysOfOperation);

        return Ok(isOperating);
    }
}