using TrainService.DTOs.SeatDTO;
using TrainService.Models;
using TrainService.Repositories.SeatRepository;
using TrainService.Exceptions;
using TrainService.Repositories.ScheduleRepository;

namespace TrainService.Services.SeatService;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IScheduleRepository _scheduleRepository;

    public SeatService(ISeatRepository seatRepository, IScheduleRepository scheduleRepository)
    {
        _seatRepository = seatRepository;
        _scheduleRepository = scheduleRepository;
    }

    // public async Task<List<SeatResponseDto>> GetByTrainAndScheduleAsync(
    //     int trainId,
    //     int scheduleId)
    // {
    //     var seats = await _seatRepository
    //         .GetByTrainAndScheduleAsync(trainId, scheduleId);

    //     return seats.Select(MapToDto).ToList();
    // }
    public async Task<List<SeatResponseDto>> GetByTrainAndScheduleAsync(
    int trainId,
    int scheduleId,
    DateOnly journeyDate,
    decimal? startDistanceKm = null,
    decimal? endDistanceKm = null)
    {
        await ValidateJourneyDateAsync(scheduleId, journeyDate);

        var seats = await _seatRepository
            .GetByTrainAndScheduleAsync(
                trainId,
                scheduleId,
                journeyDate);

        var result = new List<SeatResponseDto>();

        foreach (var seat in seats)
        {
            var availability = await _seatRepository.GetAvailabilityAsync(seat.SeatId, journeyDate);
            var isAvailable = startDistanceKm.HasValue && endDistanceKm.HasValue
                ? (seat.StartDistanceKm == null || (seat.StartDistanceKm <= startDistanceKm && seat.EndDistanceKm >= endDistanceKm)) && !await _seatRepository.HasOverlappingReservationAsync(seat.SeatId, journeyDate, startDistanceKm.Value, endDistanceKm.Value) && (availability?.IsAvailable ?? true)
                : availability?.IsAvailable ?? true;

            result.Add(new SeatResponseDto
            {
                SeatId = seat.SeatId,
                TrainId = seat.TrainId,
                ScheduleId = seat.ScheduleId,
                SeatNumber = seat.SeatNumber,
                ClassType = seat.ClassType,
                IsAvailable = isAvailable,
                StartDistanceKm = seat.StartDistanceKm,
                EndDistanceKm = seat.EndDistanceKm
            });
        }

        return result;
    }

    public async Task<SeatResponseDto> GetByIdAsync(int seatId)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);

        if (seat == null)
        {
            throw new ResourceNotFoundException(
                $"Seat with ID {seatId} was not found.");
        }

        return MapToDto(seat);
    }

    public async Task<SeatResponseDto> AddAsync(
        int trainId,
        int scheduleId,
        SeatRequestDto request)
    {
        var existingSeat = await _seatRepository
            .GetBySeatNumberAsync(
                trainId,
                scheduleId,
                request.SeatNumber);

        if (existingSeat != null)
        {
            throw new DuplicateResourceException("A seat with this number already exists for this schedule.");
        }

        if (request.StartDistanceKm.HasValue != request.EndDistanceKm.HasValue ||
            request.StartDistanceKm.HasValue && request.EndDistanceKm <= request.StartDistanceKm)
            throw new BusinessRuleException("Seat route limits must include a valid start and end distance.");

        await ValidateSeatRouteAsync(trainId, request.StartDistanceKm, request.EndDistanceKm);

        var seat = new Seat
        {
            TrainId = trainId,
            ScheduleId = scheduleId,
            SeatNumber = request.SeatNumber,
            ClassType = request.ClassType,
            IsAvailable = true,
            StartDistanceKm = request.StartDistanceKm,
            EndDistanceKm = request.EndDistanceKm
        };

        await _seatRepository.AddAsync(seat);

        return MapToDto(seat);
    }

    public async Task<SeatResponseDto> UpdateAsync(
        int seatId,
        SeatRequestDto request)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);

        if (seat == null)
        {
            throw new ResourceNotFoundException(
                $"Seat with ID {seatId} was not found.");
        }

        var existingSeat = await _seatRepository
            .GetBySeatNumberAsync(
                seat.TrainId,
                seat.ScheduleId,
                request.SeatNumber);

        if (existingSeat != null && existingSeat.SeatId != seatId)
        {
            throw new DuplicateResourceException("A seat with this number already exists for this schedule.");
        }

        seat.SeatNumber = request.SeatNumber;
        seat.ClassType = request.ClassType;
        if (request.StartDistanceKm.HasValue != request.EndDistanceKm.HasValue ||
            request.StartDistanceKm.HasValue && request.EndDistanceKm <= request.StartDistanceKm)
            throw new BusinessRuleException("Seat route limits must include a valid start and end distance.");
        await ValidateSeatRouteAsync(seat.TrainId, request.StartDistanceKm, request.EndDistanceKm);
        seat.StartDistanceKm = request.StartDistanceKm;
        seat.EndDistanceKm = request.EndDistanceKm;

        await _seatRepository.UpdateAsync(seat);

        return MapToDto(seat);
    }

    public async Task<bool> DeleteAsync(int seatId)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);

        if (seat == null)
        {
            throw new ResourceNotFoundException(
                $"Seat with ID {seatId} was not found.");
        }

        await _seatRepository.DeleteAsync(seatId);

        return true;
    }

    public async Task<bool> ReserveAsync(
    int seatId,
    DateOnly journeyDate,
    decimal? startDistanceKm = null,
    decimal? endDistanceKm = null)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);

        if (seat == null)
            return false;

        await ValidateJourneyDateAsync(
            seat.ScheduleId,
            journeyDate);

        if (startDistanceKm.HasValue && endDistanceKm.HasValue)
        {
            if (startDistanceKm.Value < 0 || endDistanceKm.Value <= startDistanceKm.Value) return false;
            if (seat.StartDistanceKm.HasValue &&
                (seat.StartDistanceKm.Value > startDistanceKm.Value || seat.EndDistanceKm!.Value < endDistanceKm.Value)) return false;
            var sectionAvailability = await _seatRepository.GetAvailabilityAsync(seatId, journeyDate);
            if (sectionAvailability != null && !sectionAvailability.IsAvailable) return false;

            return await _seatRepository.TryAddReservationAsync(new SeatReservation
            {
                SeatId = seatId,
                JourneyDate = journeyDate,
                StartDistanceKm = startDistanceKm.Value,
                EndDistanceKm = endDistanceKm.Value
            });
        }

        var availability =
            await _seatRepository.GetAvailabilityAsync(
                seatId,
                journeyDate);

        if (availability != null && !availability.IsAvailable)
            return false;

        if (availability == null)
        {
            await _seatRepository.AddAvailabilityAsync(
                new SeatAvailability
                {
                    SeatId = seatId,
                    JourneyDate = journeyDate,
                    IsAvailable = false
                });
        }
        else
        {
            availability.IsAvailable = false;

            await _seatRepository.UpdateAvailabilityAsync(
                availability);
        }

        return true;
    }

    public async Task<bool> ReleaseAsync(
    int seatId,
    DateOnly journeyDate,
    decimal? startDistanceKm = null,
    decimal? endDistanceKm = null)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);

        if (seat == null)
            return false;

        await ValidateJourneyDateAsync(
            seat.ScheduleId,
            journeyDate);

        if (startDistanceKm.HasValue && endDistanceKm.HasValue)
        {
            return await _seatRepository.RemoveReservationAsync(seatId, journeyDate, startDistanceKm.Value, endDistanceKm.Value);
        }

        var availability =
            await _seatRepository.GetAvailabilityAsync(
                seatId,
                journeyDate);

        if (availability == null)
            return false;

        availability.IsAvailable = true;

        await _seatRepository.UpdateAvailabilityAsync(
            availability);

        return true;
    }

    private static SeatResponseDto MapToDto(Seat seat)
    {
        return new SeatResponseDto
        {
            SeatId = seat.SeatId,
            TrainId = seat.TrainId,
            ScheduleId = seat.ScheduleId,
            SeatNumber = seat.SeatNumber,
            ClassType = seat.ClassType,
            IsAvailable = seat.IsAvailable,
            StartDistanceKm = seat.StartDistanceKm,
            EndDistanceKm = seat.EndDistanceKm
        };
    }

    private async Task ValidateJourneyDateAsync(
    int scheduleId,
    DateOnly journeyDate)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);

        if (schedule == null)
        {
            throw new ResourceNotFoundException(
                $"Schedule with ID {scheduleId} was not found.");
        }

        var requestedDay = journeyDate.DayOfWeek
            .ToString()
            .Substring(0, 3);

        var operatingDays = schedule.DaysOfOperation
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim());

        if (!operatingDays.Contains(
                requestedDay,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                $"The train does not operate on {requestedDay}.");
        }
    }

    private async Task ValidateSeatRouteAsync(int trainId, decimal? startDistanceKm, decimal? endDistanceKm)
    {
        if (!startDistanceKm.HasValue || !endDistanceKm.HasValue) return;
        var train = await _seatRepository.GetTrainRouteAsync(trainId)
            ?? throw new ResourceNotFoundException($"Train with ID {trainId} was not found.");
        var stops = train.RouteStations.Select(station => station.DistanceFromOriginKm)
            .Append(0)
            .Append(train.TotalDistanceKm)
            .ToHashSet();
        if (!stops.Contains(startDistanceKm.Value) || !stops.Contains(endDistanceKm.Value))
            throw new BusinessRuleException("Seat route limits must match stations configured on this train.");
    }
}