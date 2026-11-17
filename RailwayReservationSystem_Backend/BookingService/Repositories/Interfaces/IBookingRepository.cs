using BookingService.Models;

namespace BookingService.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int bookingId);

    Task<Booking?> GetByPNRAsync(string pnr);

    Task<List<Booking>> GetByUserIdAsync(int userId);

    Task AddAsync(Booking booking);

    Task UpdateAsync(Booking booking);

    Task<bool> ExistsByPNRAsync(string pnr);

    Task<bool> AddAndConsumePaymentAsync(Booking booking, Payment payment);

    Task<Payment?> GetPaymentAsync(string paymentId, int userId);
        Task UpdatePaymentAsync(Payment payment);

    Task AddPaymentAsync(Payment payment);
}