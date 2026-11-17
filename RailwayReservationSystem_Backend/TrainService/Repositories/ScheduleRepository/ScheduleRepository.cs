using Microsoft.EntityFrameworkCore;
using TrainService.Data;
using TrainService.Models;

namespace TrainService.Repositories.ScheduleRepository;

public class ScheduleRepository : IScheduleRepository
{
    private readonly TrainDbContext _context;

    public ScheduleRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<List<Schedule>> GetByTrainIdAsync(int trainId)
    {
        return await _context.Schedules
            .Where(s => s.TrainId == trainId)
            .ToListAsync();
    }

    public async Task<Schedule?> GetByIdAsync(int scheduleId)
    {
        return await _context.Schedules
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
    }

    public async Task AddAsync(Schedule schedule)
    {
        await _context.Schedules.AddAsync(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Schedule schedule)
    {
        _context.Schedules.Update(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int scheduleId)
    {
        var schedule = await GetByIdAsync(scheduleId);

        if (schedule != null)
        {
            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }
}