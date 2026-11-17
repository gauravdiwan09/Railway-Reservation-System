using TrainService.DTOs.FareDTO;
using TrainService.Models;
using TrainService.Repositories.FareRepository;
using TrainService.Exceptions;

namespace TrainService.Services.FareService;

public class FareService : IFareService
{
    private readonly IFareRepository _fareRepository;

    public FareService(IFareRepository fareRepository)
    {
        _fareRepository = fareRepository;
    }

    public async Task<List<FareResponseDto>> GetByTrainIdAsync(int trainId)
    {
        var fares = await _fareRepository.GetByTrainIdAsync(trainId);

        return fares.Select(MapToDto).ToList();
    }

    public async Task<FareResponseDto> GetByIdAsync(int fareId)
    {
        var fare = await _fareRepository.GetByIdAsync(fareId);

        if (fare == null)
        {
            throw new ResourceNotFoundException(
                $"Fare with ID {fareId} was not found.");
        }

        return MapToDto(fare);
    }

    public async Task<FareResponseDto> AddAsync(
        int trainId,
        FareRequestDto request)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Fare amount must be greater than zero.");
        }

        var existingFare =
            await _fareRepository.GetByTrainIdAndClassTypeAsync(
                trainId,
                request.ClassType);

        if (existingFare != null)
        {
            throw new DuplicateResourceException("A fare for this class already exists for this train.");
        }

        var fare = new Fare
        {
            TrainId = trainId,
            ClassType = request.ClassType,
            Amount = request.Amount
        };

        await _fareRepository.AddAsync(fare);

        return MapToDto(fare);
    }

    public async Task<FareResponseDto> UpdateAsync(
        int fareId,
        FareRequestDto request)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessRuleException("Fare amount must be greater than zero.");
        }

        var fare = await _fareRepository.GetByIdAsync(fareId);

        if (fare == null)
        {
            throw new ResourceNotFoundException(
                $"Seat with ID {fareId} was not found.");
        }

        var existingFare =
            await _fareRepository.GetByTrainIdAndClassTypeAsync(
                fare.TrainId,
                request.ClassType);

        if (existingFare != null && existingFare.FareId != fareId)
        {
             throw new DuplicateResourceException("A fare for this class already exists for this train.");
        }

        fare.ClassType = request.ClassType;
        fare.Amount = request.Amount;

        await _fareRepository.UpdateAsync(fare);

        return MapToDto(fare);
    }

    public async Task<bool> DeleteAsync(int fareId)
    {
        var fare = await _fareRepository.GetByIdAsync(fareId);

        if (fare == null)
        {
            throw new ResourceNotFoundException(
                $"Fare with ID {fareId} was not found.");
        }

        await _fareRepository.DeleteAsync(fareId);

        return true;
    }

    private static FareResponseDto MapToDto(Fare fare)
    {
        return new FareResponseDto
        {
            FareId = fare.FareId,
            TrainId = fare.TrainId,
            ClassType = fare.ClassType,
            Amount = fare.Amount
        };
    }
}