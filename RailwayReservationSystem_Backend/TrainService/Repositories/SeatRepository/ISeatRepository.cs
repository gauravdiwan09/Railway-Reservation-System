using TrainService.Models;

namespace TrainService.Repositories.SeatRepository;

public interface ISeatRepository
{
    Task<List<Seat>> GetByTrainAndScheduleAsync(int trainId, int scheduleId);
    Task<Seat?> GetByIdAsync(int seatId);
    Task<Train?> GetTrainRouteAsync(int trainId);
    Task<Seat?> GetBySeatNumberAsync(
        int trainId,
        int scheduleId,
        string seatNumber);

    Task AddAsync(Seat seat);
    Task UpdateAsync(Seat seat);
    Task DeleteAsync(int seatId);
    Task<List<Seat>> GetByTrainAndScheduleAsync(
    int trainId,
    int scheduleId,
    DateOnly journeyDate);
    Task<SeatAvailability?> GetAvailabilityAsync(
        int seatId,
        DateOnly journeyDate);
    Task AddAvailabilityAsync(SeatAvailability availability);
    Task UpdateAvailabilityAsync(SeatAvailability availability);
    Task<bool> HasOverlappingReservationAsync(int seatId, DateOnly journeyDate, decimal startDistanceKm, decimal endDistanceKm);
    Task AddReservationAsync(SeatReservation reservation);
    Task<bool> TryAddReservationAsync(SeatReservation reservation);
    Task<bool> RemoveReservationAsync(int seatId, DateOnly journeyDate, decimal startDistanceKm, decimal endDistanceKm);
}