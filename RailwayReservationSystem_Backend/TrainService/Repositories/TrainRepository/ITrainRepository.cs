using TrainService.Models;

namespace TrainService.Repositories;

public interface ITrainRepository
{
    Task<List<Train>> GetAllAsync();

    Task<Train?> GetByIdAsync(int trainId);

    Task<Train?> GetByTrainNumberAsync(string trainNumber);

    Task AddAsync(Train train);

    Task UpdateAsync(Train train);

    Task DeleteAsync(int trainId);

    Task<List<Train>> SearchAsync(string source, string destination);
}