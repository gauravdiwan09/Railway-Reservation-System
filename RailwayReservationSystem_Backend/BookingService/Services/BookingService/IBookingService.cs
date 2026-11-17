using BookingService.DTOs;

namespace BookingService.Services.BookingService;

public interface IBookingService
{
    Task<BookingResponseDto> CreateBookingAsync(
        CreateBookingRequestDto request,
        int userId);

        Task<RazorpayOrderResponseDto> CreateRazorpayOrderAsync(CreateRazorpayOrderRequestDto request, int userId);

        Task<RazorpayPaymentResponseDto> VerifyRazorpayPaymentAsync(VerifyRazorpayPaymentRequestDto request, int userId);

    Task<BookingResponseDto> GetBookingByIdAsync(int bookingId);

    Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId);

    Task<BookingResponseDto> GetBookingByPNRAsync(string pnr);

    Task<BookingCancellationResponseDto> CancelBookingAsync(int bookingId);
}