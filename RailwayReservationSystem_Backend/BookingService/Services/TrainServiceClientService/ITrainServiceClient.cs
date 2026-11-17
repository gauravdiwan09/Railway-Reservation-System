using BookingService.DTOs;

namespace BookingService.Services.TrainServiceClientService;

public interface ITrainServiceClient
{
    Task<decimal?> GetFareAsync(int trainId, string classType);

    // Task<List<SeatResponse>> GetAvailableSeatsAsync(
    //     int trainId,
    //     int scheduleId,
    //     string classType);

    // Task<bool> ReserveSeatAsync(int seatId);

    // Task<bool> ReleaseSeatAsync(int seatId);

    Task<List<SeatResponse>> GetAvailableSeatsAsync(
    int trainId,
    int scheduleId,
    string classType,
    DateOnly journeyDate,
    decimal startDistanceKm = 0,
    decimal endDistanceKm = 0);

    Task<bool> ReserveSeatAsync(
        int seatId,
        DateOnly journeyDate,
        decimal startDistanceKm = 0,
        decimal endDistanceKm = 0);

    Task<bool> ReleaseSeatAsync(
        int seatId,
        DateOnly journeyDate,
        decimal startDistanceKm = 0,
        decimal endDistanceKm = 0);

    Task<bool> IsScheduleAvailableAsync(
    int trainId,
    int scheduleId,
    DateOnly journeyDate);

    Task<TrainResponseDto?> GetTrainAsync(int trainId);
}