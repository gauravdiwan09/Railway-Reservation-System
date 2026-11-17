using BookingService.Repositories.Interfaces;
using BookingService.Services.RazorpayClientService;
using BookingService.Services.TrainServiceClientService;
using BookingService.Services.PnrGeneratorService;
using BookingService.DTOs;
using BookingService.Exceptions;

namespace BookingService.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IPnrGeneratorService _pnrGeneratorService;
    private readonly ITrainServiceClient _trainServiceClient;
    private readonly IRazorpayClient _razorpayClient;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepository,
        IPnrGeneratorService pnrGeneratorService,
        ITrainServiceClient trainServiceClient,
        IRazorpayClient razorpayClient,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _pnrGeneratorService = pnrGeneratorService;
        _trainServiceClient = trainServiceClient;
        _razorpayClient = razorpayClient;
        _logger = logger;
    }

        public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequestDto request, int userId)
    {
        if (request.TrainId <= 0)
            throw new InvalidBookingException("Invalid train ID.");

        if (request.ScheduleId <= 0)
            throw new InvalidBookingException("Invalid schedule ID.");

        if (request.JourneyDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidBookingException("Journey date cannot be in the past.");

        if (string.IsNullOrWhiteSpace(request.ClassType))
            throw new InvalidBookingException("Class type is required.");

        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new InvalidBookingException("Complete mock payment before confirming the booking.");

        if (request.Passengers == null || request.Passengers.Count == 0)
            throw new InvalidBookingException("At least one passenger is required.");

        if (request.Passengers.Count > 6)
            throw new InvalidBookingException("A maximum of 6 passengers is allowed per booking.");


        foreach (var passenger in request.Passengers)
        {
            if (string.IsNullOrWhiteSpace(passenger.Name))
                throw new InvalidBookingException("Passenger name is required.");

            if (passenger.Age <= 0)
                throw new InvalidBookingException("Passenger age must be greater than 0.");

            if (string.IsNullOrWhiteSpace(passenger.Gender))
                throw new InvalidBookingException("Passenger gender is required.");
        }

        var scheduleAvailable = await _trainServiceClient.IsScheduleAvailableAsync(
            request.TrainId,
            request.ScheduleId,
            request.JourneyDate);

        if (!scheduleAvailable)
        {
            throw new InvalidBookingException(
                "Train does not operate on the selected journey date.");
        }

        var train = await _trainServiceClient.GetTrainAsync(request.TrainId)
            ?? throw new InvalidBookingException("Train not found.");
        var (startDistance, endDistance) = ResolveJourneySegment(train, request.Source, request.Destination);
        var totalFare = await CalculateFareAsync(
            request.TrainId,
            request.ClassType,
            request.Passengers.Count,
            startDistance,
            endDistance,
            train.TotalDistanceKm);
        var payment = await _bookingRepository.GetPaymentAsync(request.PaymentId, userId);
        if (payment == null || payment.Provider != "Razorpay" || payment.Status != "Captured" || payment.IsUsed || payment.Amount != totalFare ||
            string.IsNullOrWhiteSpace(payment.RazorpayPaymentId) ||
            payment.TrainId != request.TrainId || payment.ScheduleId != request.ScheduleId || payment.JourneyDate != request.JourneyDate ||
            payment.Source != request.Source || payment.Destination != request.Destination || payment.ClassType != request.ClassType ||
            payment.PassengerCount != request.Passengers.Count)
            throw new InvalidBookingException("Payment is invalid, already used, or does not match this booking total.");

        var providerPayment = await _razorpayClient.GetPaymentAsync(payment.RazorpayPaymentId);
        if (providerPayment.OrderId != payment.ProviderReference || providerPayment.Amount != checked(decimal.ToInt64(payment.Amount * 100)) ||
            providerPayment.Currency != "INR" || providerPayment.Status != "captured")
            throw new InvalidBookingException("Razorpay no longer reports this payment as captured.");

        var availableSeats = await _trainServiceClient.GetAvailableSeatsAsync(
        request.TrainId,
        request.ScheduleId,
        request.ClassType,
        request.JourneyDate,
        startDistance,
        endDistance);

        if (availableSeats.Count < request.Passengers.Count)
        {
            await RefundPaymentAsync(payment);
            throw new BookingConflictException("Not enough seats available. Your Razorpay payment has been refunded.");
        }

        var selectedSeats = availableSeats
        .Take(request.Passengers.Count)
        .ToList();

        var reservedSeats = new List<int>();

        foreach (var seat in selectedSeats)
        {
            var reserved = await _trainServiceClient.ReserveSeatAsync(seat.SeatId, request.JourneyDate, startDistance, endDistance);

            if (!reserved)
            {
                foreach (var reservedSeatId in reservedSeats)
                {
                    await _trainServiceClient.ReleaseSeatAsync(reservedSeatId, request.JourneyDate, startDistance, endDistance);
                }

                await RefundPaymentAsync(payment);

                throw new BookingConflictException(
                    "Unable to reserve the selected seats. Your Razorpay payment has been refunded.");
            }

            reservedSeats.Add(seat.SeatId);
        }

        var pnr = await _pnrGeneratorService.GenerateAsync();

        var booking = new Models.Booking
        {
            PNR = pnr,
            PaymentId = payment.PaymentId,
            UserId = userId,
            TrainId = request.TrainId,
            ScheduleId = request.ScheduleId,
            ClassType = request.ClassType,
            BookingDate = DateTime.UtcNow,
            JourneyDate = request.JourneyDate,
            Source = request.Source,
            Destination = request.Destination,
            StartDistanceKm = startDistance,
            EndDistanceKm = endDistance,
            Status = "Confirmed",
            NumberOfTickets = request.Passengers.Count,
            TotalFare = totalFare
        };

        for (int i = 0; i < request.Passengers.Count; i++)
        {
            var passengerRequest = request.Passengers[i];
            var seat = selectedSeats[i];

            booking.Passengers.Add(new Models.Passenger
            {
                Name = passengerRequest.Name,
                Age = passengerRequest.Age,
                Gender = passengerRequest.Gender,
                SeatId = seat.SeatId
            });
        }

        bool bookingSaved;
        try
        {
            bookingSaved = await _bookingRepository.AddAndConsumePaymentAsync(booking, payment);
        }
        catch
        {
            foreach (var reservedSeat in reservedSeats)
                await _trainServiceClient.ReleaseSeatAsync(reservedSeat, request.JourneyDate, startDistance, endDistance);
            await RefundPaymentAsync(payment);
            throw;
        }

        if (!bookingSaved)
        {
            foreach (var reservedSeat in reservedSeats)
                await _trainServiceClient.ReleaseSeatAsync(reservedSeat, request.JourneyDate, startDistance, endDistance);
            throw new BookingConflictException("Payment has already been used. Complete a new mock payment.");
        }

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            PNR = booking.PNR,
            UserId = booking.UserId,
            TrainId = booking.TrainId,
            ScheduleId = booking.ScheduleId,
            ClassType = booking.ClassType,
            BookingDate = booking.BookingDate,
            JourneyDate = booking.JourneyDate,
            Source = booking.Source,
            Destination = booking.Destination,
            Status = booking.Status,
            NumberOfTickets = booking.NumberOfTickets,
            TotalFare = booking.TotalFare,
            Passengers = booking.Passengers.Select(p => new PassengerResponseDto
            {
                PassengerId = p.PassengerId,
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatId = p.SeatId
            }).ToList()
        };
    }

    public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new BookingNotFoundException($"Booking with ID {bookingId} was not found.");
        }

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            PNR = booking.PNR,
            UserId = booking.UserId,
            TrainId = booking.TrainId,
            ScheduleId = booking.ScheduleId,
            ClassType = booking.ClassType,
            BookingDate = booking.BookingDate,
            JourneyDate = booking.JourneyDate,
            Source = booking.Source,
            Destination = booking.Destination,
            Status = booking.Status,
            NumberOfTickets = booking.NumberOfTickets,
            TotalFare = booking.TotalFare,
            Passengers = booking.Passengers.Select(p => new PassengerResponseDto
            {
                PassengerId = p.PassengerId,
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatId = p.SeatId
            }).ToList()
        };
    }

    public async Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);

        return bookings.Select(booking => new BookingResponseDto
        {
            BookingId = booking.BookingId,
            PNR = booking.PNR,
            UserId = booking.UserId,
            TrainId = booking.TrainId,
            ScheduleId = booking.ScheduleId,
            ClassType = booking.ClassType,
            BookingDate = booking.BookingDate,
            JourneyDate = booking.JourneyDate,
            Source = booking.Source,
            Destination = booking.Destination,
            NumberOfTickets = booking.NumberOfTickets,
            Status = booking.Status,
            TotalFare = booking.TotalFare,
            Passengers = booking.Passengers.Select(p => new PassengerResponseDto
            {
                PassengerId = p.PassengerId,
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatId = p.SeatId
            }).ToList()
        }).ToList();
    }

    public async Task<BookingResponseDto> GetBookingByPNRAsync(string pnr)
    {
        var booking = await _bookingRepository.GetByPNRAsync(pnr);

        if (booking == null)
        {
            throw new BookingNotFoundException($"Booking with PNR {pnr} was not found.");
        }

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            PNR = booking.PNR,
            UserId = booking.UserId,
            TrainId = booking.TrainId,
            ScheduleId = booking.ScheduleId,
            ClassType = booking.ClassType,
            BookingDate = booking.BookingDate,
            JourneyDate = booking.JourneyDate,
            Source = booking.Source,
            Destination = booking.Destination,
            Status = booking.Status,
            NumberOfTickets = booking.NumberOfTickets,
            TotalFare = booking.TotalFare,
            Passengers = booking.Passengers.Select(p => new PassengerResponseDto
            {
                PassengerId = p.PassengerId,
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                SeatId = p.SeatId
            }).ToList()
        };
    }

    public async Task<BookingCancellationResponseDto> CancelBookingAsync(int bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking == null)
        {
            throw new BookingNotFoundException(
                $"Booking with ID {bookingId} was not found.");
        }

        var wasAlreadyCancelled = booking.Status == "Cancelled";
        if (wasAlreadyCancelled)
        {
            var existingPayment = string.IsNullOrWhiteSpace(booking.PaymentId)
                ? null
                : await _bookingRepository.GetPaymentAsync(booking.PaymentId, booking.UserId);
            if (existingPayment?.Status is not ("RefundPending" or "Captured"))
                throw new BookingConflictException("Booking is already cancelled.");
        }
        else
        {
            foreach (var passenger in booking.Passengers)
            {
                if (booking.EndDistanceKm > booking.StartDistanceKm)
                    await _trainServiceClient.ReleaseSeatAsync(passenger.SeatId, booking.JourneyDate, booking.StartDistanceKm, booking.EndDistanceKm);
                else
                    await _trainServiceClient.ReleaseSeatAsync(passenger.SeatId, booking.JourneyDate);
            }

            booking.Status = "Cancelled";
            await _bookingRepository.UpdateAsync(booking);
        }

        var refundStatus = "NotRequired";
        if (!string.IsNullOrWhiteSpace(booking.PaymentId))
        {
            var payment = await _bookingRepository.GetPaymentAsync(booking.PaymentId, booking.UserId);
            if (payment?.Status is "Captured" or "RefundPending")
            {
                try
                {
                    await RefundPaymentAsync(payment);
                    refundStatus = payment.Status;
                }
                catch (Exception exception) when (exception is RazorpayProviderException or RazorpayConfigurationException or HttpRequestException or TaskCanceledException)
                {
                    refundStatus = "RefundPending";
                    _logger.LogWarning(exception, "Booking {BookingId} was cancelled, but Razorpay refund for payment {PaymentId} is pending.", booking.BookingId, payment.PaymentId);
                }
            }
            else if (payment?.Status == "Refunded")
            {
                refundStatus = payment.Status;
            }
            else if (payment != null)
            {
                refundStatus = "NotCaptured";
            }
        }

        return new BookingCancellationResponseDto
        {
            BookingId = booking.BookingId,
            Status = booking.Status,
            RefundStatus = refundStatus
        };
    }

    public async Task<RazorpayOrderResponseDto> CreateRazorpayOrderAsync(CreateRazorpayOrderRequestDto request, int userId)
    {
        if (request.PassengerCount is < 1 or > 6)
            throw new InvalidBookingException("A payment must cover between 1 and 6 passengers.");

        if (request.JourneyDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidBookingException("Journey date cannot be in the past.");

        if (!await _trainServiceClient.IsScheduleAvailableAsync(request.TrainId, request.ScheduleId, request.JourneyDate))
            throw new InvalidBookingException("Train does not operate on the selected journey date.");

        if (string.IsNullOrWhiteSpace(request.ClassType))
            throw new InvalidBookingException("Select a travel class before payment.");

        var train = await _trainServiceClient.GetTrainAsync(request.TrainId)
            ?? throw new InvalidBookingException("Train not found.");
        var (startDistance, endDistance) = ResolveJourneySegment(train, request.Source, request.Destination);
        var amount = await CalculateFareAsync(request.TrainId, request.ClassType, request.PassengerCount, startDistance, endDistance, train.TotalDistanceKm);
        var amountInPaise = checked(decimal.ToInt64(amount * 100));
        var internalPaymentId = Guid.NewGuid().ToString("N");
        var order = await _razorpayClient.CreateOrderAsync(amountInPaise, internalPaymentId, new
        {
            trainId = request.TrainId,
            scheduleId = request.ScheduleId,
            journeyDate = request.JourneyDate.ToString("yyyy-MM-dd"),
            source = request.Source,
            destination = request.Destination
        });
        if (order.Amount != amountInPaise || order.Currency != "INR")
            throw new RazorpayProviderException("Razorpay returned an order that did not match the requested amount.");

        var payment = new Models.Payment
        {
            PaymentId = internalPaymentId,
            UserId = userId,
            TrainId = request.TrainId,
            ScheduleId = request.ScheduleId,
            JourneyDate = request.JourneyDate,
            Source = request.Source,
            Destination = request.Destination,
            ClassType = request.ClassType,
            PassengerCount = request.PassengerCount,
            Amount = amount,
            Provider = "Razorpay",
            ProviderReference = order.OrderId,
            Status = "OrderCreated",
            CreatedAt = DateTime.UtcNow
        };

        await _bookingRepository.AddPaymentAsync(payment);
        return new RazorpayOrderResponseDto
        {
            PaymentId = payment.PaymentId,
            OrderId = order.OrderId,
            KeyId = _razorpayClient.KeyId,
            Amount = amountInPaise,
            Currency = order.Currency
        };
    }

    public async Task<RazorpayPaymentResponseDto> VerifyRazorpayPaymentAsync(VerifyRazorpayPaymentRequestDto request, int userId)
    {
        var payment = await _bookingRepository.GetPaymentAsync(request.PaymentId, userId)
            ?? throw new InvalidBookingException("Payment order was not found for this account.");

        if (payment.Provider != "Razorpay" || payment.Status != "OrderCreated" || payment.IsUsed ||
            !string.Equals(payment.ProviderReference, request.OrderId, StringComparison.Ordinal))
            throw new InvalidBookingException("Payment order is invalid, already verified, or does not belong to this account.");

        if (!_razorpayClient.VerifySignature(request.OrderId, request.RazorpayPaymentId, request.RazorpaySignature))
            throw new InvalidBookingException("Razorpay payment signature verification failed.");

        var expectedAmount = checked(decimal.ToInt64(payment.Amount * 100));
        var providerPayment = await _razorpayClient.GetPaymentAsync(request.RazorpayPaymentId);
        if (providerPayment.OrderId != request.OrderId || providerPayment.Amount != expectedAmount || providerPayment.Currency != "INR")
            throw new InvalidBookingException("Razorpay payment details do not match the order.");

        if (providerPayment.Status == "authorized")
            providerPayment = await _razorpayClient.CapturePaymentAsync(request.RazorpayPaymentId, expectedAmount, "INR");

        if (providerPayment.Status != "captured" || providerPayment.OrderId != request.OrderId ||
            providerPayment.Amount != expectedAmount || providerPayment.Currency != "INR")
            throw new InvalidBookingException("Razorpay payment has not been captured.");

        payment.RazorpayPaymentId = providerPayment.PaymentId;
        payment.Status = "Captured";
        await _bookingRepository.UpdatePaymentAsync(payment);

        return new RazorpayPaymentResponseDto
        {
            PaymentId = payment.PaymentId,
            Status = payment.Status,
            Amount = payment.Amount
        };
    }

    private async Task<decimal> CalculateFareAsync(int trainId, string classType, int passengerCount, decimal startDistance, decimal endDistance, decimal totalDistance)
    {
        var fare = await _trainServiceClient.GetFareAsync(trainId, classType)
            ?? throw new InvalidBookingException("Fare not available for the selected train and class.");
        if (totalDistance <= 0) throw new InvalidBookingException("Train route distance is not configured.");
        var perPassengerFare = decimal.Round(fare * (endDistance - startDistance) / totalDistance, 2, MidpointRounding.AwayFromZero);
        return perPassengerFare * passengerCount;
    }

    private async Task RefundPaymentAsync(Models.Payment payment)
    {
        if (payment.RazorpayPaymentId == null) return;
        var amountInPaise = checked(decimal.ToInt64(payment.Amount * 100));
        payment.Status = "RefundPending";
        await _bookingRepository.UpdatePaymentAsync(payment);
        await _razorpayClient.RefundPaymentAsync(payment.RazorpayPaymentId, amountInPaise);
        payment.Status = "Refunded";
        await _bookingRepository.UpdatePaymentAsync(payment);
    }

    private static (decimal StartDistance, decimal EndDistance) ResolveJourneySegment(TrainResponseDto train, string source, string destination)
    {
        var stops = new[] { (Name: train.Source, Distance: 0m, Sequence: 0) }
            .Concat(train.IntermediateStations.Select((stop, index) => (Name: stop.StationName, Distance: stop.DistanceFromOriginKm, Sequence: index + 1)))
            .Append((Name: train.Destination, Distance: train.TotalDistanceKm, Sequence: train.IntermediateStations.Count + 1))
            .ToList();
        var start = stops.FirstOrDefault(stop => string.Equals(stop.Name.Trim(), source.Trim(), StringComparison.OrdinalIgnoreCase));
        var end = stops.FirstOrDefault(stop => string.Equals(stop.Name.Trim(), destination.Trim(), StringComparison.OrdinalIgnoreCase));
        if (start.Sequence >= end.Sequence || end.Distance <= start.Distance)
            throw new InvalidBookingException("Selected stations are not a valid forward segment on this train.");
        return (start.Distance, end.Distance);
    }
}