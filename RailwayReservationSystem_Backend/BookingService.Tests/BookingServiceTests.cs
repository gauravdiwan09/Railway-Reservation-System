using BookingService.DTOs;
using BookingService.Exceptions;
using BookingService.Models;
using BookingService.Repositories.Interfaces;
using BookingService.Repositories;
using BookingService.Services.BookingService;
using BookingService.Services.PnrGeneratorService;
using BookingService.Services.TicketPdfGeneratorService;
using BookingService.Services.TrainServiceClientService;
using BookingService.Services.RazorpayClientService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BookingService.Tests;

public class BookingServiceTests
{
    private Mock<IBookingRepository> _bookingRepositoryMock = null!;
    private Mock<ITrainServiceClient> _trainServiceClientMock = null!;
    private Mock<IPnrGeneratorService> _pnrGeneratorServiceMock = null!;
    private Mock<IRazorpayClient> _razorpayClientMock = null!;

    private Services.BookingService.BookingService _bookingService = null!;

    [SetUp]
    public void Setup()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _trainServiceClientMock = new Mock<ITrainServiceClient>();
        _pnrGeneratorServiceMock = new Mock<IPnrGeneratorService>();
        _razorpayClientMock = new Mock<IRazorpayClient>();
        _razorpayClientMock.SetupGet(client => client.KeyId).Returns("rzp_test_public");
        _razorpayClientMock
            .Setup(client => client.GetPaymentAsync("pay_verified"))
            .ReturnsAsync(new RazorpayPaymentDetails("pay_verified", "order_1", 70000, "INR", "captured"));
        _razorpayClientMock
            .Setup(client => client.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<long>()))
            .Returns(Task.CompletedTask);

        _trainServiceClientMock
            .Setup(client => client.GetTrainAsync(It.IsAny<int>()))
            .ReturnsAsync(new TrainResponseDto
            {
                TrainId = 1,
                Source = "A",
                Destination = "B",
                TotalDistanceKm = 100
            });
        _bookingRepositoryMock
            .Setup(repository => repository.GetPaymentAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string paymentId, int userId) => new Payment
            {
                PaymentId = paymentId,
                UserId = userId,
                Provider = "Razorpay",
                ProviderReference = "order_1",
                RazorpayPaymentId = "pay_verified",
                TrainId = 1,
                ScheduleId = 1,
                JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Source = "A",
                Destination = "B",
                ClassType = "SL",
                PassengerCount = 2,
                Amount = 700,
                Status = "Captured"
            });

        _bookingService =
            new Services.BookingService.BookingService(
                _bookingRepositoryMock.Object,
                _pnrGeneratorServiceMock.Object,
                _trainServiceClientMock.Object,
                _razorpayClientMock.Object,
                NullLogger<Services.BookingService.BookingService>.Instance);
    }

    [Test]
    public async Task GetBookingByIdAsync_WhenBookingExists_ReturnsBooking()
    {
        // Arrange
        var booking = new Booking
        {
            BookingId = 1,
            PNR = "PNR123456",
            UserId = 10,
            TrainId = 1,
            ScheduleId = 1,
            ClassType = "SL",
            BookingDate = DateTime.UtcNow,
            JourneyDate = new DateOnly(2026, 9, 20),
            StartDistanceKm = 0,
            EndDistanceKm = 100,
            Status = "Confirmed",
            NumberOfTickets = 1,
            TotalFare = 350,
            Passengers = new List<Passenger>
            {
                new Passenger
                {
                    PassengerId = 1,
                    BookingId = 1,
                    Name = "Gaurav",
                    Age = 22,
                    Gender = "Male",
                    SeatId = 5
                }
            }
        };

        _bookingRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BookingId, Is.EqualTo(1));
        Assert.That(result.PNR, Is.EqualTo("PNR123456"));
        Assert.That(result.UserId, Is.EqualTo(10));
        Assert.That(result.Passengers.Count, Is.EqualTo(1));

        _bookingRepositoryMock.Verify(
            r => r.GetByIdAsync(1),
            Times.Once);
    }

    [Test]
    public async Task GetBookingByIdAsync_WhenBookingDoesNotExist_ThrowsBookingNotFoundException()
    {
        // Arrange
        _bookingRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Booking?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<BookingNotFoundException>(
            async () => await _bookingService.GetBookingByIdAsync(999));

        Assert.That(
            exception!.Message,
            Does.Contain("999"));

        _bookingRepositoryMock.Verify(
            r => r.GetByIdAsync(999),
            Times.Once);
    }

    [Test]
    public async Task CancelBookingAsync_WhenBookingExists_ReleasesSeatsAndUpdatesBooking()
    {
        // Arrange
        var booking = new Booking
        {
            BookingId = 1,
            PNR = "PNR123456",
            UserId = 10,
            TrainId = 1,
            ScheduleId = 1,
            ClassType = "SL",
            BookingDate = DateTime.UtcNow,
            JourneyDate = new DateOnly(2026, 9, 20),
            StartDistanceKm = 0,
            EndDistanceKm = 100,
            Status = "Confirmed",
            NumberOfTickets = 2,
            TotalFare = 700,
            Passengers = new List<Passenger>
            {
                new Passenger
                {
                    PassengerId = 1,
                    BookingId = 1,
                    Name = "Gaurav",
                    Age = 22,
                    Gender = "Male",
                    SeatId = 5
                },
                new Passenger
                {
                    PassengerId = 2,
                    BookingId = 1,
                    Name = "Rahul",
                    Age = 23,
                    Gender = "Male",
                    SeatId = 6
                }
            }
        };

        _bookingRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _trainServiceClientMock
            .Setup(t => t.ReleaseSeatAsync(5, booking.JourneyDate, 0, 100))
            .ReturnsAsync(true);

        _trainServiceClientMock
            .Setup(t => t.ReleaseSeatAsync(6, booking.JourneyDate, 0, 100))
            .ReturnsAsync(true);

        _bookingRepositoryMock
            .Setup(r => r.UpdateAsync(booking))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _bookingService.CancelBookingAsync(1);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Cancelled"));
        Assert.That(result.RefundStatus, Is.EqualTo("NotRequired"));
        Assert.That(booking.Status, Is.EqualTo("Cancelled"));

        _trainServiceClientMock.Verify(
            t => t.ReleaseSeatAsync(5, booking.JourneyDate, 0, 100),
            Times.Once);

        _trainServiceClientMock.Verify(
            t => t.ReleaseSeatAsync(6, booking.JourneyDate, 0, 100),
            Times.Once);

        _bookingRepositoryMock.Verify(
            r => r.UpdateAsync(booking),
            Times.Once);
    }

[Test]
public async Task CancelBookingAsync_WhenBookingAlreadyCancelled_ThrowsBookingConflictException()
{
    // Arrange
    var booking = new Booking
    {
        BookingId = 2,
        UserId = 10,
        Status = "Cancelled",
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        Passengers = new List<Passenger>()
    };

    _bookingRepositoryMock
        .Setup(r => r.GetByIdAsync(2))
        .ReturnsAsync(booking);

    // Act & Assert
    var exception = Assert.ThrowsAsync<BookingConflictException>(
        async () => await _bookingService.CancelBookingAsync(2));

    Assert.That(
        exception!.Message,
        Does.Contain("cancelled"));

    _trainServiceClientMock.Verify(
        t => t.ReleaseSeatAsync(
            It.IsAny<int>(),
            It.IsAny<DateOnly>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>()),
        Times.Never);

    _bookingRepositoryMock.Verify(
        r => r.UpdateAsync(It.IsAny<Booking>()),
        Times.Never);
}

[Test]
public async Task CancelBookingAsync_WhenBookingDoesNotExist_ThrowsBookingNotFoundException()
{
    // Arrange
    _bookingRepositoryMock
        .Setup(r => r.GetByIdAsync(999))
        .ReturnsAsync((Booking?)null);

    // Act & Assert
    var exception = Assert.ThrowsAsync<BookingNotFoundException>(
        async () => await _bookingService.CancelBookingAsync(999));

    Assert.That(
        exception!.Message,
        Does.Contain("999"));

    _trainServiceClientMock.Verify(
        t => t.ReleaseSeatAsync(
            It.IsAny<int>(),
            It.IsAny<DateOnly>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>()),
        Times.Never);

    _bookingRepositoryMock.Verify(
        r => r.UpdateAsync(It.IsAny<Booking>()),
        Times.Never);
}

[Test]
public async Task CreateBookingAsync_WhenPassengerCountExceedsSix_ThrowsInvalidBookingException()
{
    // Arrange
    var request = new CreateBookingRequestDto
    {
        TrainId = 1,
        ScheduleId = 1,
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        Source = "A",
        Destination = "B",
        PaymentId = "payment",
        ClassType = "SL",
        Passengers = Enumerable.Range(1, 7)
            .Select(i => new PassengerRequestDto
            {
                Name = $"Passenger {i}",
                Age = 25,
                Gender = "Male"
            })
            .ToList()
    };

    // Act & Assert
    var exception = Assert.ThrowsAsync<InvalidBookingException>(
        async () => await _bookingService.CreateBookingAsync(request, 10));

    Assert.That(
        exception!.Message,
        Does.Contain("6"));

    _trainServiceClientMock.Verify(
        t => t.IsScheduleAvailableAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DateOnly>()),
        Times.Never);

    _trainServiceClientMock.Verify(
        t => t.GetAvailableSeatsAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<DateOnly>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>()),
        Times.Never);
}

[Test]
public async Task CreateBookingAsync_WhenTrainDoesNotOperateOnJourneyDate_ThrowsInvalidBookingException()
{
    // Arrange
    var request = new CreateBookingRequestDto
    {
        TrainId = 1,
        ScheduleId = 1,
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        Source = "A",
        Destination = "B",
        PaymentId = "payment",
        ClassType = "SL",
        Passengers = new List<PassengerRequestDto>
        {
            new PassengerRequestDto
            {
                Name = "Gaurav",
                Age = 22,
                Gender = "Male"
            }
        }
    };

    _trainServiceClientMock
        .Setup(t => t.IsScheduleAvailableAsync(
            request.TrainId,
            request.ScheduleId,
            request.JourneyDate))
        .ReturnsAsync(false);

    // Act & Assert
    var exception = Assert.ThrowsAsync<InvalidBookingException>(
        async () => await _bookingService.CreateBookingAsync(request, 10));

    Assert.That(
        exception!.Message,
        Does.Contain("does not operate"));

    _trainServiceClientMock.Verify(
        t => t.IsScheduleAvailableAsync(
            request.TrainId,
            request.ScheduleId,
            request.JourneyDate),
        Times.Once);

    _trainServiceClientMock.Verify(
        t => t.GetFareAsync(
            It.IsAny<int>(),
            It.IsAny<string>()),
        Times.Never);
}

[Test]
public async Task CreateBookingAsync_WhenNotEnoughSeatsAvailable_ThrowsBookingConflictException()
{
    // Arrange
    var request = new CreateBookingRequestDto
    {
        TrainId = 1,
        ScheduleId = 1,
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        Source = "A",
        Destination = "B",
        PaymentId = "payment",
        ClassType = "SL",
        Passengers = new List<PassengerRequestDto>
        {
            new PassengerRequestDto
            {
                Name = "Gaurav",
                Age = 22,
                Gender = "Male"
            },
            new PassengerRequestDto
            {
                Name = "Rahul",
                Age = 23,
                Gender = "Male"
            }
        }
    };

    _trainServiceClientMock
        .Setup(t => t.IsScheduleAvailableAsync(
            request.TrainId,
            request.ScheduleId,
            request.JourneyDate))
        .ReturnsAsync(true);

    _trainServiceClientMock
        .Setup(t => t.GetFareAsync(
            request.TrainId,
            request.ClassType))
        .ReturnsAsync(350m);

    _trainServiceClientMock
        .Setup(t => t.GetAvailableSeatsAsync(
            request.TrainId,
            request.ScheduleId,
            request.ClassType,
            request.JourneyDate,
            0,
            100))
        .ReturnsAsync(new List<SeatResponse>
        {
            new SeatResponse
            {
                SeatId = 1,
                TrainId = 1,
                ScheduleId = 1,
                SeatNumber = "S1",
                ClassType = "SL",
                IsAvailable = true
            }
        });

    // Act & Assert
    var exception = Assert.ThrowsAsync<BookingConflictException>(
        async () => await _bookingService.CreateBookingAsync(request, 10));

    Assert.That(
        exception!.Message,
        Does.Contain("Not enough seats"));

    _trainServiceClientMock.Verify(
        t => t.GetAvailableSeatsAsync(
            request.TrainId,
            request.ScheduleId,
            request.ClassType,
            request.JourneyDate,
            0,
            100),
        Times.Once);

    _trainServiceClientMock.Verify(
        t => t.ReserveSeatAsync(
            It.IsAny<int>(),
            It.IsAny<DateOnly>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>()),
        Times.Never);
}

[Test]
public async Task CreateBookingAsync_WhenValidRequest_CreatesBookingSuccessfully()
{
    // Arrange
    var journeyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    var request = new CreateBookingRequestDto
    {
        TrainId = 1,
        ScheduleId = 1,
        JourneyDate = journeyDate,
        Source = "A",
        Destination = "B",
        PaymentId = "payment",
        ClassType = "SL",
        Passengers = new List<PassengerRequestDto>
        {
            new PassengerRequestDto
            {
                Name = "Gaurav",
                Age = 22,
                Gender = "Male"
            },
            new PassengerRequestDto
            {
                Name = "Rahul",
                Age = 23,
                Gender = "Male"
            }
        }
    };

    _trainServiceClientMock
        .Setup(t => t.IsScheduleAvailableAsync(
            1, 1, journeyDate))
        .ReturnsAsync(true);

    _trainServiceClientMock
        .Setup(t => t.GetFareAsync(1, "SL"))
        .ReturnsAsync(350m);

    _trainServiceClientMock
        .Setup(t => t.GetAvailableSeatsAsync(1, 1, "SL", journeyDate, 0, 100))
            
        .ReturnsAsync(new List<SeatResponse>
        {
            new SeatResponse
            {
                SeatId = 1,
                TrainId = 1,
                ScheduleId = 1,
                SeatNumber = "S1",
                ClassType = "SL",
                IsAvailable = true
            },
            new SeatResponse
            {
                SeatId = 2,
                TrainId = 1,
                ScheduleId = 1,
                SeatNumber = "S2",
                ClassType = "SL",
                IsAvailable = true
            }
        });

    _trainServiceClientMock
        .Setup(t => t.ReserveSeatAsync(1, journeyDate, 0, 100))
        .ReturnsAsync(true);

    _trainServiceClientMock
        .Setup(t => t.ReserveSeatAsync(2, journeyDate, 0, 100))
        .ReturnsAsync(true);

    _pnrGeneratorServiceMock
        .Setup(p => p.GenerateAsync())
        .ReturnsAsync("PNR123456");

    Booking? savedBooking = null;

    _bookingRepositoryMock
        .Setup(r => r.AddAndConsumePaymentAsync(It.IsAny<Booking>(), It.IsAny<Payment>()))
        .Callback<Booking, Payment>((booking, _) => savedBooking = booking)
        .ReturnsAsync(true);

    // Act
    var result = await _bookingService.CreateBookingAsync(request, 10);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.PNR, Is.EqualTo("PNR123456"));
    Assert.That(result.UserId, Is.EqualTo(10));
    Assert.That(result.TrainId, Is.EqualTo(1));
    Assert.That(result.ScheduleId, Is.EqualTo(1));
    Assert.That(result.ClassType, Is.EqualTo("SL"));
    Assert.That(result.JourneyDate, Is.EqualTo(journeyDate));
    Assert.That(result.NumberOfTickets, Is.EqualTo(2));
    Assert.That(result.TotalFare, Is.EqualTo(700m));
    Assert.That(result.Status, Is.EqualTo("Confirmed"));

    Assert.That(savedBooking, Is.Not.Null);
    Assert.That(savedBooking!.Passengers.Count, Is.EqualTo(2));

    _trainServiceClientMock.Verify(
        t => t.ReserveSeatAsync(1, journeyDate, 0, 100),
        Times.Once);

    _trainServiceClientMock.Verify(
        t => t.ReserveSeatAsync(2, journeyDate, 0, 100),
        Times.Once);

    _bookingRepositoryMock.Verify(
        r => r.AddAndConsumePaymentAsync(It.IsAny<Booking>(), It.IsAny<Payment>()),
        Times.Once);
}

[Test]
public async Task CreateBookingAsync_WhenSeatReservationFails_ReleasesPreviouslyReservedSeats()
{
    // Arrange
    var journeyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    var request = new CreateBookingRequestDto
    {
        TrainId = 1,
        ScheduleId = 1,
        JourneyDate = journeyDate,
        Source = "A",
        Destination = "B",
        PaymentId = "payment",
        ClassType = "SL",
        Passengers = new List<PassengerRequestDto>
        {
            new PassengerRequestDto
            {
                Name = "Gaurav",
                Age = 22,
                Gender = "Male"
            },
            new PassengerRequestDto
            {
                Name = "Rahul",
                Age = 23,
                Gender = "Male"
            }
        }
    };

    _trainServiceClientMock
        .Setup(t => t.IsScheduleAvailableAsync(
            1, 1, journeyDate))
        .ReturnsAsync(true);

    _trainServiceClientMock
        .Setup(t => t.GetFareAsync(1, "SL"))
        .ReturnsAsync(350m);

    _trainServiceClientMock
        .Setup(t => t.GetAvailableSeatsAsync(1, 1, "SL", journeyDate, 0, 100))
        .ReturnsAsync(new List<SeatResponse>
        {
            new SeatResponse
            {
                SeatId = 1,
                TrainId = 1,
                ScheduleId = 1,
                SeatNumber = "S1",
                ClassType = "SL",
                IsAvailable = true
            },
            new SeatResponse
            {
                SeatId = 2,
                TrainId = 1,
                ScheduleId = 1,
                SeatNumber = "S2",
                ClassType = "SL",
                IsAvailable = true
            }
        });

    // First seat succeeds
    _trainServiceClientMock
        .Setup(t => t.ReserveSeatAsync(1, journeyDate, 0, 100))
        .ReturnsAsync(true);

    // Second seat fails
    _trainServiceClientMock
        .Setup(t => t.ReserveSeatAsync(2, journeyDate, 0, 100))
        .ReturnsAsync(false);

    _trainServiceClientMock
        .Setup(t => t.ReleaseSeatAsync(1, journeyDate, 0, 100))
        .ReturnsAsync(true);

    // Act & Assert
    var exception = Assert.ThrowsAsync<BookingConflictException>(
        async () => await _bookingService.CreateBookingAsync(request, 10));

    Assert.That(
        exception!.Message,
        Does.Contain("reserve"));

    // First seat must be released because second reservation failed
    _trainServiceClientMock.Verify(
        t => t.ReleaseSeatAsync(1, journeyDate, 0, 100),
        Times.Once);

    // Both reservation attempts should happen
    _trainServiceClientMock.Verify(
        t => t.ReserveSeatAsync(1, journeyDate, 0, 100),
        Times.Once);

    _trainServiceClientMock.Verify(
        t => t.ReserveSeatAsync(2, journeyDate, 0, 100),
        Times.Once);

    // Booking must never be saved
    _bookingRepositoryMock.Verify(
        r => r.AddAndConsumePaymentAsync(It.IsAny<Booking>(), It.IsAny<Payment>()),
        Times.Never);
}

[Test]
public async Task CreateRazorpayOrderAsync_ShouldProrateFareAndBindOrderToJourney()
{
    var journeyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    _trainServiceClientMock.Setup(client => client.GetTrainAsync(1)).ReturnsAsync(new TrainResponseDto
    {
        TrainId = 1,
        Source = "A",
        Destination = "C",
        TotalDistanceKm = 100,
        IntermediateStations = new List<TrainRouteStationDto>
        {
            new() { StationName = "B", DistanceFromOriginKm = 40 }
        }
    });
    _trainServiceClientMock.Setup(client => client.IsScheduleAvailableAsync(1, 8, journeyDate)).ReturnsAsync(true);
    _trainServiceClientMock.Setup(client => client.GetFareAsync(1, "SL")).ReturnsAsync(300m);
    _razorpayClientMock.Setup(client => client.CreateOrderAsync(24000, It.IsAny<string>(), It.IsAny<object>()))
        .ReturnsAsync(new RazorpayOrderResult("order_123", 24000, "INR"));
    Payment? savedPayment = null;
    _bookingRepositoryMock.Setup(repository => repository.AddPaymentAsync(It.IsAny<Payment>()))
        .Callback<Payment>(payment => savedPayment = payment)
        .Returns(Task.CompletedTask);
    var request = new CreateRazorpayOrderRequestDto
    {
        TrainId = 1,
        ScheduleId = 8,
        JourneyDate = journeyDate,
        ClassType = "SL",
        PassengerCount = 2,
        Source = "A",
        Destination = "B"
    };

    var response = await _bookingService.CreateRazorpayOrderAsync(request, 10);

    Assert.That(response.Amount, Is.EqualTo(24000));
    Assert.That(response.OrderId, Is.EqualTo("order_123"));
    Assert.That(response.KeyId, Is.EqualTo("rzp_test_public"));
    Assert.That(savedPayment, Is.Not.Null);
    Assert.That(savedPayment!.Provider, Is.EqualTo("Razorpay"));
    Assert.That(savedPayment.ProviderReference, Is.EqualTo("order_123"));
    Assert.That(savedPayment.TrainId, Is.EqualTo(1));
    Assert.That(savedPayment.ScheduleId, Is.EqualTo(8));
    Assert.That(savedPayment.JourneyDate, Is.EqualTo(journeyDate));
    Assert.That(savedPayment.Source, Is.EqualTo("A"));
    Assert.That(savedPayment.Destination, Is.EqualTo("B"));
    Assert.That(savedPayment.PassengerCount, Is.EqualTo(2));
}

[Test]
public async Task VerifyRazorpayPaymentAsync_ShouldRequireValidSignatureAndCapturedProviderPayment()
{
    var journeyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    var localPayment = new Payment
    {
        PaymentId = "local_payment",
        UserId = 10,
        Provider = "Razorpay",
        ProviderReference = "order_123",
        TrainId = 1,
        ScheduleId = 8,
        JourneyDate = journeyDate,
        Source = "A",
        Destination = "B",
        ClassType = "SL",
        PassengerCount = 1,
        Amount = 300,
        Status = "OrderCreated"
    };
    _bookingRepositoryMock.Setup(repository => repository.GetPaymentAsync("local_payment", 10)).ReturnsAsync(localPayment);
    _razorpayClientMock.Setup(client => client.VerifySignature("order_123", "pay_123", "sig_123")).Returns(true);
    _razorpayClientMock.Setup(client => client.GetPaymentAsync("pay_123"))
        .ReturnsAsync(new RazorpayPaymentDetails("pay_123", "order_123", 30000, "INR", "captured"));
    _bookingRepositoryMock.Setup(repository => repository.UpdatePaymentAsync(localPayment)).Returns(Task.CompletedTask);

    var result = await _bookingService.VerifyRazorpayPaymentAsync(new VerifyRazorpayPaymentRequestDto
    {
        PaymentId = "local_payment",
        OrderId = "order_123",
        RazorpayPaymentId = "pay_123",
        RazorpaySignature = "sig_123"
    }, 10);

    Assert.That(result.Status, Is.EqualTo("Captured"));
    Assert.That(localPayment.RazorpayPaymentId, Is.EqualTo("pay_123"));
    _bookingRepositoryMock.Verify(repository => repository.UpdatePaymentAsync(localPayment), Times.Once);
}

[Test]
public async Task CancelBookingAsync_ShouldRefundTheVerifiedRazorpayPayment()
{
    var booking = new Booking
    {
        BookingId = 20,
        UserId = 10,
        PaymentId = "local_payment",
        Status = "Confirmed",
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        StartDistanceKm = 0,
        EndDistanceKm = 100,
        Passengers = new List<Passenger>()
    };
    var payment = new Payment
    {
        PaymentId = "local_payment",
        UserId = 10,
        Provider = "Razorpay",
        RazorpayPaymentId = "pay_123",
        Amount = 300,
        Status = "Captured"
    };
    _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(20)).ReturnsAsync(booking);
    _bookingRepositoryMock.Setup(repository => repository.GetPaymentAsync("local_payment", 10)).ReturnsAsync(payment);
    _bookingRepositoryMock.Setup(repository => repository.UpdatePaymentAsync(payment)).Returns(Task.CompletedTask);
    _bookingRepositoryMock.Setup(repository => repository.UpdateAsync(booking)).Returns(Task.CompletedTask);

    var result = await _bookingService.CancelBookingAsync(20);

    Assert.That(result.Status, Is.EqualTo("Cancelled"));
    Assert.That(result.RefundStatus, Is.EqualTo("Refunded"));
    Assert.That(booking.Status, Is.EqualTo("Cancelled"));
    Assert.That(payment.Status, Is.EqualTo("Refunded"));
    _razorpayClientMock.Verify(client => client.RefundPaymentAsync("pay_123", 30000), Times.Once);
}

[Test]
public async Task CancelBookingAsync_ShouldReleaseSeatsAndCancelWhenRazorpayRefundFails()
{
    var booking = new Booking
    {
        BookingId = 21,
        UserId = 10,
        PaymentId = "local_payment",
        Status = "Confirmed",
        JourneyDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        StartDistanceKm = 0,
        EndDistanceKm = 100,
        Passengers = new List<Passenger>
        {
            new() { PassengerId = 1, BookingId = 21, SeatId = 9 }
        }
    };
    var payment = new Payment
    {
        PaymentId = "local_payment",
        UserId = 10,
        Provider = "Razorpay",
        RazorpayPaymentId = "pay_401",
        Amount = 500,
        Status = "Captured"
    };
    _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(21)).ReturnsAsync(booking);
    _bookingRepositoryMock.Setup(repository => repository.GetPaymentAsync("local_payment", 10)).ReturnsAsync(payment);
    _bookingRepositoryMock.Setup(repository => repository.UpdatePaymentAsync(payment)).Returns(Task.CompletedTask);
    _bookingRepositoryMock.Setup(repository => repository.UpdateAsync(booking)).Returns(Task.CompletedTask);
    _trainServiceClientMock.Setup(client => client.ReleaseSeatAsync(9, booking.JourneyDate, 0, 100)).ReturnsAsync(true);
    _razorpayClientMock.SetupSequence(client => client.RefundPaymentAsync("pay_401", 50000))
        .ThrowsAsync(new RazorpayProviderException("Razorpay request failed with status 401."))
        .Returns(Task.CompletedTask);

    var response = await _bookingService.CancelBookingAsync(21);

    Assert.That(response.Status, Is.EqualTo("Cancelled"));
    Assert.That(response.RefundStatus, Is.EqualTo("RefundPending"));
    Assert.That(payment.Status, Is.EqualTo("RefundPending"));
    _trainServiceClientMock.Verify(client => client.ReleaseSeatAsync(9, booking.JourneyDate, 0, 100), Times.Once);
    _bookingRepositoryMock.Verify(repository => repository.UpdateAsync(booking), Times.Once);

    var retryResponse = await _bookingService.CancelBookingAsync(21);

    Assert.That(retryResponse.Status, Is.EqualTo("Cancelled"));
    Assert.That(retryResponse.RefundStatus, Is.EqualTo("Refunded"));
    Assert.That(payment.Status, Is.EqualTo("Refunded"));
    _trainServiceClientMock.Verify(client => client.ReleaseSeatAsync(9, booking.JourneyDate, 0, 100), Times.Once);
}

[Test]
public void VerifyRazorpayPaymentAsync_ShouldRejectAnInvalidSignature()
{
    _bookingRepositoryMock.Setup(repository => repository.GetPaymentAsync("local_payment", 10))
        .ReturnsAsync(new Payment
        {
            PaymentId = "local_payment",
            UserId = 10,
            Provider = "Razorpay",
            ProviderReference = "order_123",
            Status = "OrderCreated"
        });
    _razorpayClientMock.Setup(client => client.VerifySignature("order_123", "pay_123", "bad_signature"))
        .Returns(false);

    var exception = Assert.ThrowsAsync<InvalidBookingException>(() => _bookingService.VerifyRazorpayPaymentAsync(
        new VerifyRazorpayPaymentRequestDto
        {
            PaymentId = "local_payment",
            OrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            RazorpaySignature = "bad_signature"
        }, 10));

    Assert.That(exception!.Message, Does.Contain("signature"));
    _razorpayClientMock.Verify(client => client.GetPaymentAsync(It.IsAny<string>()), Times.Never);
}
}