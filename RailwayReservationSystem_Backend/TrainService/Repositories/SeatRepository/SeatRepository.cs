using Microsoft.EntityFrameworkCore;
using System.Data;
using TrainService.Data;
using TrainService.Models;

namespace TrainService.Repositories.SeatRepository;

public class SeatRepository : ISeatRepository
{
    private readonly TrainDbContext _context;

    public SeatRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetByTrainAndScheduleAsync(
        int trainId,
        int scheduleId)
    {
        return await _context.Seats
            .Where(s =>
                s.TrainId == trainId &&
                s.ScheduleId == scheduleId)
            .ToListAsync();
    }

    public async Task<List<Seat>> GetByTrainAndScheduleAsync(
        int trainId,
        int scheduleId,
        DateOnly journeyDate)
    {
        return await _context.Seats
            .Where(s =>
                s.TrainId == trainId &&
                s.ScheduleId == scheduleId)
            .ToListAsync();
    }

    public async Task<Seat?> GetByIdAsync(int seatId)
    {
        return await _context.Seats
            .FirstOrDefaultAsync(s => s.SeatId == seatId);
    }

    public Task<Train?> GetTrainRouteAsync(int trainId)
    {
        return _context.Trains
            .Include(train => train.RouteStations)
            .FirstOrDefaultAsync(train => train.TrainId == trainId);
    }

    public async Task<Seat?> GetBySeatNumberAsync(
        int trainId,
        int scheduleId,
        string seatNumber)
    {
        return await _context.Seats
            .FirstOrDefaultAsync(s =>
                s.TrainId == trainId &&
                s.ScheduleId == scheduleId &&
                s.SeatNumber == seatNumber);
    }

    public async Task AddAsync(Seat seat)
    {
        await _context.Seats.AddAsync(seat);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Seat seat)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int seatId)
    {
        var seat = await GetByIdAsync(seatId);

        if (seat != null)
        {
            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SeatAvailability?> GetAvailabilityAsync(
    int seatId,
    DateOnly journeyDate)
    {
        return await _context.SeatAvailabilities
            .FirstOrDefaultAsync(sa =>
                sa.SeatId == seatId &&
                sa.JourneyDate == journeyDate);
    }

    public async Task AddAvailabilityAsync(
    SeatAvailability availability)
    {
        await _context.SeatAvailabilities.AddAsync(availability);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAvailabilityAsync(
    SeatAvailability availability)
    {
        _context.SeatAvailabilities.Update(availability);
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasOverlappingReservationAsync(
        int seatId,
        DateOnly journeyDate,
        decimal startDistanceKm,
        decimal endDistanceKm)
    {
        return _context.SeatReservations.AnyAsync(reservation =>
            reservation.SeatId == seatId &&
            reservation.JourneyDate == journeyDate &&
            reservation.StartDistanceKm < endDistanceKm &&
            reservation.EndDistanceKm > startDistanceKm);
    }

    public async Task AddReservationAsync(SeatReservation reservation)
    {
        await _context.SeatReservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryAddReservationAsync(SeatReservation reservation)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var overlaps = await _context.SeatReservations.AnyAsync(existing =>
            existing.SeatId == reservation.SeatId &&
            existing.JourneyDate == reservation.JourneyDate &&
            existing.StartDistanceKm < reservation.EndDistanceKm &&
            existing.EndDistanceKm > reservation.StartDistanceKm);
        if (overlaps)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _context.SeatReservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> RemoveReservationAsync(
        int seatId,
        DateOnly journeyDate,
        decimal startDistanceKm,
        decimal endDistanceKm)
    {
        var reservation = await _context.SeatReservations.FirstOrDefaultAsync(item =>
            item.SeatId == seatId &&
            item.JourneyDate == journeyDate &&
            item.StartDistanceKm == startDistanceKm &&
            item.EndDistanceKm == endDistanceKm);

        if (reservation == null) return false;
        _context.SeatReservations.Remove(reservation);
        await _context.SaveChangesAsync();
        return true;
    }
}