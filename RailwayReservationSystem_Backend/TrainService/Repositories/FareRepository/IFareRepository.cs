using TrainService.Models;

namespace TrainService.Repositories.FareRepository;

public interface IFareRepository
{
    Task<List<Fare>> GetByTrainIdAsync(int trainId);
    Task<Fare?> GetByIdAsync(int fareId);
    Task<Fare?> GetByTrainIdAndClassTypeAsync(int trainId, string classType);
    Task AddAsync(Fare fare);
    Task UpdateAsync(Fare fare);
    Task DeleteAsync(int fareId);
}