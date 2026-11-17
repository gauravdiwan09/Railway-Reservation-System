using TrainService.DTOs.TrainDTO;

namespace TrainService.Services.TrainService;

public interface ITrainService
{
    Task<List<TrainResponseDto>> GetAllAsync();

    Task<TrainResponseDto> GetByIdAsync(int trainId);

    Task<TrainResponseDto> AddAsync(TrainRequestDto request);

    Task<TrainResponseDto> UpdateAsync(int trainId, TrainRequestDto request);

    Task<bool> DeleteAsync(int trainId);

    Task<List<TrainSearchResponseDto>> SearchAsync(string source, string destination, DateTime date);
}