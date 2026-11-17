using Microsoft.EntityFrameworkCore;
using TrainService.Data;
using TrainService.Models;

namespace TrainService.Repositories.FareRepository;

public class FareRepository : IFareRepository
{
    private readonly TrainDbContext _context;

    public FareRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<List<Fare>> GetByTrainIdAsync(int trainId)
    {
        return await _context.Fares
            .Where(f => f.TrainId == trainId)
            .ToListAsync();
    }

    public async Task<Fare?> GetByIdAsync(int fareId)
    {
        return await _context.Fares
            .FirstOrDefaultAsync(f => f.FareId == fareId);
    }

    public async Task<Fare?> GetByTrainIdAndClassTypeAsync(
        int trainId,
        string classType)
    {
        return await _context.Fares
            .FirstOrDefaultAsync(f =>
                f.TrainId == trainId &&
                f.ClassType == classType);
    }

    public async Task AddAsync(Fare fare)
    {
        await _context.Fares.AddAsync(fare);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Fare fare)
    {
        _context.Fares.Update(fare);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int fareId)
    {
        var fare = await GetByIdAsync(fareId);

        if (fare != null)
        {
            _context.Fares.Remove(fare);
            await _context.SaveChangesAsync();
        }
    }
}