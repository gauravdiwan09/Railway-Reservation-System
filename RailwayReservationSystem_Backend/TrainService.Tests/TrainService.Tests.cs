using Moq;
using NUnit.Framework;
using TrainService.DTOs.FareDTO;
using TrainService.DTOs.TrainDTO;
using TrainService.DTOs.SeatDTO;
using TrainService.DTOs.ScheduleDTO;
using TrainService.Models;
using TrainService.Repositories.FareRepository;
using TrainService.Repositories.ScheduleRepository;
using TrainService.Repositories.SeatRepository;
using TrainService.Repositories;
using TrainService.Services.FareService;
using TrainService.Services.ScheduleService;
using TrainService.Services.SeatService;
using TrainService.Exceptions;

namespace TrainService.Tests.Services;

[TestFixture]
public class TrainServiceTests
{
    private Mock<ITrainRepository> _trainRepositoryMock = null!;
    private TrainService.Services.TrainService.TrainService _trainService = null!;

    [SetUp]
    public void Setup()
    {
        _trainRepositoryMock = new Mock<ITrainRepository>();

        _trainService =
            new TrainService.Services.TrainService.TrainService(
                _trainRepositoryMock.Object);
    }

    // Tests will go here
    [Test]
public async Task GetAllAsync_ShouldReturnAllTrains()
{
    // Arrange
    var trains = new List<Train>
    {
        new Train
        {
            TrainId = 1,
            TrainNumber = "101",
            TrainName = "Express 1",
            Source = "Bhopal",
            Destination = "Delhi",
            TotalSeats = 100
        },
        new Train
        {
            TrainId = 2,
            TrainNumber = "102",
            TrainName = "Express 2",
            Source = "Indore",
            Destination = "Mumbai",
            TotalSeats = 120
        }
    };

    _trainRepositoryMock
        .Setup(r => r.GetAllAsync())
        .ReturnsAsync(trains);

    // Act
    var result = await _trainService.GetAllAsync();

    // Assert
    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0].TrainId, Is.EqualTo(1));
    Assert.That(result[1].TrainNumber, Is.EqualTo("102"));

    _trainRepositoryMock.Verify(
        r => r.GetAllAsync(),
        Times.Once);
}

[Test]
public async Task GetByIdAsync_ShouldReturnTrain_WhenTrainExists()
{
    // Arrange
    var train = new Train
    {
        TrainId = 1,
        TrainNumber = "101",
        TrainName = "Express 1",
        Source = "Bhopal",
        Destination = "Delhi",
        TotalSeats = 100
    };

    _trainRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(train);

    // Act
    var result = await _trainService.GetByIdAsync(1);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result!.TrainId, Is.EqualTo(1));
    Assert.That(result.TrainNumber, Is.EqualTo("101"));
    Assert.That(result.Source, Is.EqualTo("Bhopal"));

    _trainRepositoryMock.Verify(
        r => r.GetByIdAsync(1),
        Times.Once);
}

[Test]
public async Task GetByIdAsync_ShouldThrowResourceNotFoundException_WhenTrainDoesNotExist()
{
    // Arrange
    _trainRepositoryMock
        .Setup(r => r.GetByIdAsync(999))
        .ReturnsAsync((Train?)null);

    // Act & Assert
    var exception = Assert.ThrowsAsync<ResourceNotFoundException>(
        async () => await _trainService.GetByIdAsync(999));

    Assert.That(
        exception!.Message,
        Is.EqualTo("Train with ID 999 was not found."));

    _trainRepositoryMock.Verify(
        r => r.GetByIdAsync(999),
        Times.Once);
}

[Test]
public async Task AddAsync_ShouldAddTrain_WhenTrainNumberIsUnique()
{
    // Arrange
    var request = new TrainRequestDto
    {
        TrainNumber = "101",
        TrainName = "Express 1",
        Source = "Bhopal",
        Destination = "Delhi",
        TotalSeats = 100
    };

    _trainRepositoryMock
        .Setup(r => r.GetByTrainNumberAsync(request.TrainNumber))
        .ReturnsAsync((Train?)null);

    _trainRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Train>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _trainService.AddAsync(request);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.TrainNumber, Is.EqualTo("101"));
    Assert.That(result.TrainName, Is.EqualTo("Express 1"));

    _trainRepositoryMock.Verify(
        r => r.GetByTrainNumberAsync("101"),
        Times.Once);

    _trainRepositoryMock.Verify(
        r => r.AddAsync(It.Is<Train>(t =>
            t.TrainNumber == "101" &&
            t.Source == "Bhopal" &&
            t.Destination == "Delhi")),
        Times.Once);
}

[Test]
public void AddAsync_ShouldThrowDuplicateResourceException_WhenTrainNumberAlreadyExists()
{
    // Arrange
    var request = new TrainRequestDto
    {
        TrainNumber = "101",
        TrainName = "Express 1",
        Source = "Bhopal",
        Destination = "Delhi",
        TotalSeats = 100
    };

    var existingTrain = new Train
    {
        TrainId = 1,
        TrainNumber = "101",
        TrainName = "Existing Train"
    };

    _trainRepositoryMock
        .Setup(r => r.GetByTrainNumberAsync("101"))
        .ReturnsAsync(existingTrain);

    // Act & Assert
    var exception = Assert.ThrowsAsync<DuplicateResourceException>(
        async () => await _trainService.AddAsync(request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("A train with this train number already exists."));

    _trainRepositoryMock.Verify(
        r => r.GetByTrainNumberAsync("101"),
        Times.Once);

    _trainRepositoryMock.Verify(
        r => r.AddAsync(It.IsAny<Train>()),
        Times.Never);
}

[Test]
public async Task UpdateAsync_ShouldUpdateTrain_WhenTrainExists()
{
    // Arrange
    var existingTrain = new Train
    {
        TrainId = 1,
        TrainNumber = "101",
        TrainName = "Old Name",
        Source = "Bhopal",
        Destination = "Delhi",
        TotalSeats = 100
    };

    var request = new TrainRequestDto
    {
        TrainNumber = "101",
        TrainName = "Updated Express",
        Source = "Bhopal",
        Destination = "Mumbai",
        TotalSeats = 120
    };

    _trainRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(existingTrain);

    _trainRepositoryMock
        .Setup(r => r.GetByTrainNumberAsync("101"))
        .ReturnsAsync(existingTrain);

    _trainRepositoryMock
        .Setup(r => r.UpdateAsync(It.IsAny<Train>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _trainService.UpdateAsync(1, request);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result!.TrainName, Is.EqualTo("Updated Express"));
    Assert.That(result.Destination, Is.EqualTo("Mumbai"));
    Assert.That(result.TotalSeats, Is.EqualTo(120));

    _trainRepositoryMock.Verify(
        r => r.GetByIdAsync(1),
        Times.Once);

    _trainRepositoryMock.Verify(
        r => r.UpdateAsync(It.Is<Train>(t =>
            t.TrainId == 1 &&
            t.TrainName == "Updated Express" &&
            t.Destination == "Mumbai" &&
            t.TotalSeats == 120)),
        Times.Once);
}

[Test]
public void ScheduleAdd_ShouldThrowBusinessRuleException_WhenArrivalTimeIsBeforeDepartureTime()
{
    // Arrange
    var scheduleRepositoryMock = new Mock<IScheduleRepository>();

    var scheduleService = new ScheduleService(
        scheduleRepositoryMock.Object);

    var request = new ScheduleRequestDto
    {
        DepartureTime = new TimeSpan(14, 0, 0),
        ArrivalTime = new TimeSpan(12, 0, 0),
        ArrivalNextDay = false,
        DaysOfOperation = "Wed"
    };

    // Act & Assert
    var exception = Assert.ThrowsAsync<BusinessRuleException>(
        async () => await scheduleService.AddAsync(1, request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("Arrival time must be after departure time."));

    scheduleRepositoryMock.Verify(
        r => r.AddAsync(It.IsAny<Schedule>()),
        Times.Never);
}

[Test]
public void FareAdd_ShouldThrowDuplicateResourceException_WhenFareAlreadyExists()
{
    // Arrange
    var fareRepositoryMock = new Mock<IFareRepository>();

    var fareService = new FareService(
        fareRepositoryMock.Object);

    var existingFare = new Fare
    {
        FareId = 1,
        TrainId = 10,
        ClassType = "AC",
        Amount = 1500
    };

    var request = new FareRequestDto
    {
        ClassType = "AC",
        Amount = 1800
    };

    fareRepositoryMock
        .Setup(r => r.GetByTrainIdAndClassTypeAsync(10, "AC"))
        .ReturnsAsync(existingFare);

    // Act & Assert
    var exception = Assert.ThrowsAsync<DuplicateResourceException>(
        async () => await fareService.AddAsync(10, request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("A fare for this class already exists for this train."));

    fareRepositoryMock.Verify(
        r => r.GetByTrainIdAndClassTypeAsync(10, "AC"),
        Times.Once);

    fareRepositoryMock.Verify(
        r => r.AddAsync(It.IsAny<Fare>()),
        Times.Never);
}

[Test]
public async Task ReserveAsync_ShouldCreateAvailability_WhenSeatIsAvailableForJourneyDate()
{
    // Arrange
    var seatRepositoryMock = new Mock<ISeatRepository>();
    var scheduleRepositoryMock = new Mock<IScheduleRepository>();

    var seatService = new SeatService(
        seatRepositoryMock.Object,
        scheduleRepositoryMock.Object);

    var journeyDate = new DateOnly(2026, 9, 16);

    var seat = new Seat
    {
        SeatId = 1,
        TrainId = 10,
        ScheduleId = 1,
        SeatNumber = "A1",
        ClassType = "AC",
        IsAvailable = true
    };

    var schedule = new Schedule
    {
        ScheduleId = 1,
        TrainId = 10,
        DaysOfOperation = "Wed"
    };

    seatRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(seat);

    scheduleRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(schedule);

    seatRepositoryMock
        .Setup(r => r.GetAvailabilityAsync(1, journeyDate))
        .ReturnsAsync((SeatAvailability?)null);

    seatRepositoryMock
        .Setup(r => r.AddAvailabilityAsync(
            It.IsAny<SeatAvailability>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await seatService.ReserveAsync(
        1,
        journeyDate);

    // Assert
    Assert.That(result, Is.True);

    seatRepositoryMock.Verify(
        r => r.GetAvailabilityAsync(1, journeyDate),
        Times.Once);

    seatRepositoryMock.Verify(
        r => r.AddAvailabilityAsync(
            It.Is<SeatAvailability>(a =>
                a.SeatId == 1 &&
                a.JourneyDate == journeyDate &&
                a.IsAvailable == false)),
        Times.Once);
}

[Test]
public async Task ReserveAsync_ShouldReturnFalse_WhenSeatIsAlreadyReservedForJourneyDate()
{
    // Arrange
    var seatRepositoryMock = new Mock<ISeatRepository>();
    var scheduleRepositoryMock = new Mock<IScheduleRepository>();

    var seatService = new SeatService(
        seatRepositoryMock.Object,
        scheduleRepositoryMock.Object);

    var journeyDate = new DateOnly(2026, 9, 16);

    var seat = new Seat
    {
        SeatId = 1,
        TrainId = 10,
        ScheduleId = 1,
        SeatNumber = "A1",
        ClassType = "AC",
        IsAvailable = true
    };

    var schedule = new Schedule
    {
        ScheduleId = 1,
        TrainId = 10,
        DaysOfOperation = "Wed"
    };

    var availability = new SeatAvailability
    {
        SeatAvailabilityId = 1,
        SeatId = 1,
        JourneyDate = journeyDate,
        IsAvailable = false
    };

    seatRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(seat);

    scheduleRepositoryMock
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(schedule);

    seatRepositoryMock
        .Setup(r => r.GetAvailabilityAsync(1, journeyDate))
        .ReturnsAsync(availability);

    // Act
    var result = await seatService.ReserveAsync(
        1,
        journeyDate);

    // Assert
    Assert.That(result, Is.False);

    seatRepositoryMock.Verify(
        r => r.GetAvailabilityAsync(1, journeyDate),
        Times.Once);

    seatRepositoryMock.Verify(
        r => r.AddAvailabilityAsync(
            It.IsAny<SeatAvailability>()),
        Times.Never);

    seatRepositoryMock.Verify(
        r => r.UpdateAvailabilityAsync(
            It.IsAny<SeatAvailability>()),
        Times.Never);
}

[Test]
public async Task SearchAsync_ShouldReturnForwardIntermediateStationSegmentsWithDistances()
{
    var date = new DateTime(2026, 10, 19);
    var train = new Train
    {
        TrainId = 5,
        TrainNumber = "12345",
        TrainName = "Coastal Express",
        Source = "Mangaluru",
        Destination = "Bengaluru",
        TotalSeats = 100,
        TotalDistanceKm = 420,
        RouteStations = new List<RouteStation>
        {
            new() { StationName = "Udupi", Sequence = 1, DistanceFromOriginKm = 120 },
            new() { StationName = "Mysuru", Sequence = 2, DistanceFromOriginKm = 330 }
        },
        Schedules = new List<Schedule>
        {
            new() { ScheduleId = 8, DepartureTime = new TimeSpan(8, 0, 0), ArrivalTime = new TimeSpan(16, 0, 0), DaysOfOperation = "Mon" }
        }
    };
    _trainRepositoryMock.Setup(repository => repository.SearchAsync("Udupi", "Bengaluru"))
        .ReturnsAsync(new List<Train> { train });

    var results = await _trainService.SearchAsync("Udupi", "Bengaluru", date);

    Assert.That(results, Has.Count.EqualTo(1));
    Assert.That(results[0].Source, Is.EqualTo("Udupi"));
    Assert.That(results[0].Destination, Is.EqualTo("Bengaluru"));
    Assert.That(results[0].StartDistanceKm, Is.EqualTo(120));
    Assert.That(results[0].EndDistanceKm, Is.EqualTo(420));
}

[Test]
public async Task ReserveAsync_ShouldStoreTheRequestedRouteSection()
{
    var seatRepositoryMock = new Mock<ISeatRepository>();
    var scheduleRepositoryMock = new Mock<IScheduleRepository>();
    var seatService = new SeatService(seatRepositoryMock.Object, scheduleRepositoryMock.Object);
    var journeyDate = new DateOnly(2026, 9, 16);
    var seat = new Seat { SeatId = 1, TrainId = 10, ScheduleId = 1, SeatNumber = "A1", ClassType = "AC", IsAvailable = true, StartDistanceKm = 0, EndDistanceKm = 120 };
    seatRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(seat);
    seatRepositoryMock.Setup(repository => repository.GetAvailabilityAsync(1, journeyDate)).ReturnsAsync((SeatAvailability?)null);
    seatRepositoryMock.Setup(repository => repository.TryAddReservationAsync(It.IsAny<SeatReservation>())).ReturnsAsync(true);
    scheduleRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(new Schedule { ScheduleId = 1, TrainId = 10, DaysOfOperation = "Wed" });

    var result = await seatService.ReserveAsync(1, journeyDate, 0, 120);

    Assert.That(result, Is.True);
    seatRepositoryMock.Verify(repository => repository.TryAddReservationAsync(It.Is<SeatReservation>(reservation =>
        reservation.SeatId == 1 && reservation.StartDistanceKm == 0 && reservation.EndDistanceKm == 120)), Times.Once);
}

[Test]
public async Task ReleaseAsync_ShouldRemoveTheMatchingRouteSectionReservation()
{
    var seatRepositoryMock = new Mock<ISeatRepository>();
    var scheduleRepositoryMock = new Mock<IScheduleRepository>();
    var seatService = new SeatService(seatRepositoryMock.Object, scheduleRepositoryMock.Object);
    var journeyDate = new DateOnly(2026, 9, 16);
    seatRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(new Seat
    {
        SeatId = 1,
        TrainId = 10,
        ScheduleId = 1,
        SeatNumber = "A1",
        ClassType = "AC",
        IsAvailable = true
    });
    scheduleRepositoryMock.Setup(repository => repository.GetByIdAsync(1))
        .ReturnsAsync(new Schedule { ScheduleId = 1, TrainId = 10, DaysOfOperation = "Wed" });
    seatRepositoryMock.Setup(repository => repository.RemoveReservationAsync(1, journeyDate, 0, 120))
        .ReturnsAsync(true);

    var released = await seatService.ReleaseAsync(1, journeyDate, 0, 120);

    Assert.That(released, Is.True);
    seatRepositoryMock.Verify(repository => repository.RemoveReservationAsync(1, journeyDate, 0, 120), Times.Once);
}
}