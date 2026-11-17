using Microsoft.EntityFrameworkCore;
using TrainService.Data;
using TrainService.Models;

namespace TrainService.Repositories;

public class TrainRepository : ITrainRepository
{
    private readonly TrainDbContext _context;

    public TrainRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<List<Train>> GetAllAsync()
    {
        return await _context.Trains
            .Include(t => t.RouteStations)
            .ToListAsync();
    }

    public async Task<Train?> GetByIdAsync(int trainId)
    {
        return await _context.Trains
            .Include(t => t.RouteStations)
            .FirstOrDefaultAsync(t => t.TrainId == trainId);
    }

    public async Task<Train?> GetByTrainNumberAsync(string trainNumber)
    {
        return await _context.Trains
            .FirstOrDefaultAsync(t => t.TrainNumber == trainNumber);
    }

    public async Task AddAsync(Train train)
    {
        await _context.Trains.AddAsync(train);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Train train)
    {
        _context.Trains.Update(train);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int trainId)
    {
        var train = await _context.Trains
            .FirstOrDefaultAsync(t => t.TrainId == trainId);

        if (train != null)
        {
            _context.Trains.Remove(train);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Train>> SearchAsync(string source, string destination)
    {
        return await _context.Trains
            .Include(t => t.Schedules)
            .Include(t => t.RouteStations)
            .Where(train =>
                (train.Source.ToLower() == source.Trim().ToLower() || train.RouteStations.Any(station => station.StationName.ToLower() == source.Trim().ToLower())) &&
                (train.Destination.ToLower() == destination.Trim().ToLower() || train.RouteStations.Any(station => station.StationName.ToLower() == destination.Trim().ToLower())))
            .ToListAsync();
    }
}