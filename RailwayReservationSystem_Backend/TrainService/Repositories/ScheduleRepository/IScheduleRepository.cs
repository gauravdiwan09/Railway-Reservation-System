using TrainService.Models;

namespace TrainService.Repositories.ScheduleRepository;

public interface IScheduleRepository
{
    Task<List<Schedule>> GetByTrainIdAsync(int trainId);
    Task<Schedule?> GetByIdAsync(int scheduleId);
    Task AddAsync(Schedule schedule);
    Task UpdateAsync(Schedule schedule);
    Task DeleteAsync(int scheduleId);
}