using TrainService.DTOs.ScheduleDTO;

namespace TrainService.Services.ScheduleService;

public interface IScheduleService
{
    Task<List<ScheduleResponseDto>> GetByTrainIdAsync(int trainId);
    Task<ScheduleResponseDto> GetByIdAsync(int scheduleId);
    Task<ScheduleResponseDto> AddAsync(
        int trainId,
        ScheduleRequestDto request);
    Task<ScheduleResponseDto> UpdateAsync(
        int scheduleId,
        ScheduleRequestDto request);
    Task<bool> DeleteAsync(int scheduleId);
}