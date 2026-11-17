using TrainService.DTOs.SeatDTO;

namespace TrainService.Services.SeatService;

public interface ISeatService
{
    Task<List<SeatResponseDto>> GetByTrainAndScheduleAsync(
        int trainId,
        int scheduleId,
        DateOnly journeyDate,
        decimal? startDistanceKm = null,
        decimal? endDistanceKm = null);

    Task<SeatResponseDto> GetByIdAsync(int seatId);

    Task<SeatResponseDto> AddAsync(
        int trainId,
        int scheduleId,
        SeatRequestDto request);

    Task<SeatResponseDto> UpdateAsync(
        int seatId,
        SeatRequestDto request);

    Task<bool> DeleteAsync(int seatId);

    Task<bool> ReserveAsync(int seatId, DateOnly journeyDate, decimal? startDistanceKm = null, decimal? endDistanceKm = null);

    Task<bool> ReleaseAsync(int seatId, DateOnly journeyDate, decimal? startDistanceKm = null, decimal? endDistanceKm = null);
}