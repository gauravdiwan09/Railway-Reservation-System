using TrainService.DTOs.ScheduleDTO;
using TrainService.Models;
using TrainService.Repositories.ScheduleRepository;
using TrainService.Exceptions;

namespace TrainService.Services.ScheduleService;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleService(IScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<List<ScheduleResponseDto>> GetByTrainIdAsync(int trainId)
    {
        var schedules = await _scheduleRepository.GetByTrainIdAsync(trainId);

        return schedules.Select(MapToDto).ToList();
    }

    public async Task<ScheduleResponseDto> GetByIdAsync(int scheduleId)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);

        if (schedule == null)
        {
            throw new ResourceNotFoundException(
                $"Schedule with ID {scheduleId} was not found.");
        }

        return MapToDto(schedule);
    }

    public async Task<ScheduleResponseDto> AddAsync(
        int trainId,
        ScheduleRequestDto request)
    {
        if (!request.ArrivalNextDay && request.ArrivalTime <= request.DepartureTime)
        {
            throw new BusinessRuleException("Arrival time must be after departure time.");
        }

        var schedule = new Schedule
        {
            TrainId = trainId,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            ArrivalNextDay = request.ArrivalNextDay,
            DaysOfOperation = request.DaysOfOperation
        };

        await _scheduleRepository.AddAsync(schedule);

        return MapToDto(schedule);
    }

    public async Task<ScheduleResponseDto> UpdateAsync(
        int scheduleId,
        ScheduleRequestDto request)
    {
        if (!request.ArrivalNextDay && request.ArrivalTime <= request.DepartureTime)
        {
            throw new BusinessRuleException("Arrival time must be after departure time.");
        }

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);

        if (schedule == null)
        {
            throw new ResourceNotFoundException(
                $"Schedule with ID {scheduleId} was not found.");
        }

        schedule.DepartureTime = request.DepartureTime;
        schedule.ArrivalTime = request.ArrivalTime;
        schedule.ArrivalNextDay = request.ArrivalNextDay;
        schedule.DaysOfOperation = request.DaysOfOperation;

        await _scheduleRepository.UpdateAsync(schedule);

        return MapToDto(schedule);
    }

    public async Task<bool> DeleteAsync(int scheduleId)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);

        if (schedule == null)
        {
            throw new ResourceNotFoundException(
                $"Schedule with ID {scheduleId} was not found.");
        }

        await _scheduleRepository.DeleteAsync(scheduleId);

        return true;
    }

    private static ScheduleResponseDto MapToDto(Schedule schedule)
    {
        return new ScheduleResponseDto
        {
            ScheduleId = schedule.ScheduleId,
            TrainId = schedule.TrainId,
            DepartureTime = schedule.DepartureTime,
            ArrivalTime = schedule.ArrivalTime,
            ArrivalNextDay = schedule.ArrivalNextDay,
            DaysOfOperation = schedule.DaysOfOperation
        };
    }
}