using TrainService.DTOs.FareDTO;

namespace TrainService.Services.FareService;

public interface IFareService
{
    Task<List<FareResponseDto>> GetByTrainIdAsync(int trainId);
    Task<FareResponseDto> GetByIdAsync(int fareId);
    Task<FareResponseDto> AddAsync(int trainId, FareRequestDto request);
    Task<FareResponseDto> UpdateAsync(int fareId, FareRequestDto request);
    Task<bool> DeleteAsync(int fareId);
}