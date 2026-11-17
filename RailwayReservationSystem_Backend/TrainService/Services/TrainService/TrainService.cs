using TrainService.DTOs.TrainDTO;
using TrainService.Models;
using TrainService.Repositories;
using TrainService.Exceptions;

namespace TrainService.Services.TrainService;

public class TrainService : ITrainService
{
    private readonly ITrainRepository _trainRepository;

    public TrainService(ITrainRepository trainRepository)
    {
        _trainRepository = trainRepository;
    }

    public async Task<List<TrainResponseDto>> GetAllAsync()
    {
        var trains = await _trainRepository.GetAllAsync();

        return trains.Select(t => new TrainResponseDto
        {
            TrainId = t.TrainId,
            TrainNumber = t.TrainNumber,
            TrainName = t.TrainName,
            Source = t.Source,
            Destination = t.Destination,
            TotalSeats = t.TotalSeats,
            TotalDistanceKm = EffectiveTotalDistance(t),
            IntermediateStations = MapStations(t)
        }).ToList();
    }

    public async Task<TrainResponseDto> GetByIdAsync(int trainId)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);
        
        if (train == null)
        {
            throw new ResourceNotFoundException(
                $"Train with ID {trainId} was not found.");
        }

        return MapToDto(train);
    }

    public async Task<TrainResponseDto> AddAsync(TrainRequestDto request)
    {
        var existingTrain =
            await _trainRepository.GetByTrainNumberAsync(request.TrainNumber);

        if (existingTrain != null)
        {
            throw new DuplicateResourceException("A train with this train number already exists.");
        }

        var train = new Train
        {
            TrainNumber = request.TrainNumber,
            TrainName = request.TrainName,
            Source = request.Source,
            Destination = request.Destination,
            TotalSeats = request.TotalSeats,
            TotalDistanceKm = ResolveTotalDistance(request),
            RouteStations = CreateStations(request)
        };

        await _trainRepository.AddAsync(train);

        return MapToDto(train);
    }

    public async Task<TrainResponseDto> UpdateAsync(
        int trainId,
        TrainRequestDto request)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);

        if (train == null)
        {
            throw new ResourceNotFoundException(
                $"Train with ID {trainId} was not found.");
        }

        var existingTrain =
            await _trainRepository.GetByTrainNumberAsync(request.TrainNumber);

        if (existingTrain != null && existingTrain.TrainId != trainId)
        {
            throw new DuplicateResourceException("A train with this train number already exists.");
        }

        train.TrainNumber = request.TrainNumber;
        train.TrainName = request.TrainName;
        train.Source = request.Source;
        train.Destination = request.Destination;
        train.TotalSeats = request.TotalSeats;
        train.TotalDistanceKm = ResolveTotalDistance(request);
        train.RouteStations.Clear();
        foreach (var station in CreateStations(request))
        {
            train.RouteStations.Add(station);
        }

        await _trainRepository.UpdateAsync(train);

        return MapToDto(train);
    }

    public async Task<bool> DeleteAsync(int trainId)
    {
        var train = await _trainRepository.GetByIdAsync(trainId);

        if (train == null)
        {
            throw new ResourceNotFoundException(
                $"Train with ID {trainId} was not found.");
        }

        await _trainRepository.DeleteAsync(trainId);

        return true;
    }

    private static TrainResponseDto MapToDto(Train train)
    {
        return new TrainResponseDto
        {
            TrainId = train.TrainId,
            TrainNumber = train.TrainNumber,
            TrainName = train.TrainName,
            Source = train.Source,
            Destination = train.Destination,
            TotalSeats = train.TotalSeats,
            TotalDistanceKm = EffectiveTotalDistance(train),
            IntermediateStations = MapStations(train)
        };
    }

    private static List<RouteStation> CreateStations(TrainRequestDto request)
    {
        return request.IntermediateStations
            .Select((station, index) => new RouteStation
            {
                StationName = station.StationName.Trim(),
                Sequence = index + 1,
                DistanceFromOriginKm = station.DistanceFromOriginKm
            })
            .ToList();
    }

    private static List<RouteStationRequestDto> MapStations(Train train)
    {
        return train.RouteStations
            .OrderBy(station => station.Sequence)
            .Select(station => new RouteStationRequestDto
            {
                StationName = station.StationName,
                DistanceFromOriginKm = station.DistanceFromOriginKm
            })
            .ToList();
    }

    private static decimal ResolveTotalDistance(TrainRequestDto request)
    {
        var finalIntermediateDistance = request.IntermediateStations
            .Select(station => station.DistanceFromOriginKm)
            .DefaultIfEmpty(0)
            .Max();
        return request.TotalDistanceKm > finalIntermediateDistance
            ? request.TotalDistanceKm
            : finalIntermediateDistance + 1;
    }

    private static decimal EffectiveTotalDistance(Train train) =>
        train.TotalDistanceKm > 0
            ? train.TotalDistanceKm
            : Math.Max(train.RouteStations.Select(station => station.DistanceFromOriginKm).DefaultIfEmpty(0).Max() + 1, 1);

    public async Task<List<TrainSearchResponseDto>> SearchAsync(string source, string destination, 
    DateTime date)
    {
        var trains = await _trainRepository.SearchAsync(
            source,
            destination);

        var requestedDay = date.DayOfWeek.ToString().Substring(0,3);

        return trains
            .SelectMany(train =>
            {
                var stops = new[]
                    {
                        (Name: train.Source, Distance: 0m, Sequence: 0)
                    }
                    .Concat(train.RouteStations.OrderBy(station => station.Sequence)
                        .Select(station => (Name: station.StationName, Distance: station.DistanceFromOriginKm, Sequence: station.Sequence)))
                    .Append((Name: train.Destination, Distance: EffectiveTotalDistance(train), Sequence: train.RouteStations.Count + 1))
                    .ToList();
                var start = stops.FirstOrDefault(stop => string.Equals(stop.Name.Trim(), source.Trim(), StringComparison.OrdinalIgnoreCase));
                var end = stops.FirstOrDefault(stop => string.Equals(stop.Name.Trim(), destination.Trim(), StringComparison.OrdinalIgnoreCase));
                if (start.Name == null || end.Name == null || start.Sequence >= end.Sequence)
                {
                    return Enumerable.Empty<TrainSearchResponseDto>();
                }

                return train.Schedules
                .Where(schedule =>
                    schedule.DaysOfOperation
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(day => day.Trim())
                        .Any(day =>
                            string.Equals(
                                day,
                                requestedDay,
                                StringComparison.OrdinalIgnoreCase)))
                .Select(schedule => new TrainSearchResponseDto
                {
                    TrainId = train.TrainId,
                    TrainNumber = train.TrainNumber,
                    TrainName = train.TrainName,
                    Source = start.Name,
                    Destination = end.Name,
                    TotalSeats = train.TotalSeats,
                    ScheduleId = schedule.ScheduleId,
                    DepartureTime = schedule.DepartureTime,
                    ArrivalTime = schedule.ArrivalTime,
                    ArrivalNextDay = schedule.ArrivalNextDay,
                    StartDistanceKm = start.Distance,
                    EndDistanceKm = end.Distance,
                    TotalDistanceKm = EffectiveTotalDistance(train)
                });
            })
            .ToList();
    }
}