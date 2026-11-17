using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BookingService.Repositories.Interfaces;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);
    }

    public async Task<Booking?> GetByPNRAsync(string pnr)
    {
        return await _context.Bookings
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.PNR == pnr);
    }

    public async Task<List<Booking>> GetByUserIdAsync(int userId)
    {
        return await _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByPNRAsync(string pnr)
    {
        return await _context.Bookings
            .AnyAsync(b => b.PNR == pnr);
    }

    public async Task<bool> AddAndConsumePaymentAsync(Booking booking, Payment payment)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var updatedPayments = await _context.Payments
            .Where(item => item.PaymentId == payment.PaymentId && item.UserId == payment.UserId && item.Provider == "Razorpay" && item.Status == "Captured" && item.RazorpayPaymentId != null && !item.IsUsed && item.Amount == payment.Amount && item.TrainId == payment.TrainId && item.ScheduleId == payment.ScheduleId && item.JourneyDate == payment.JourneyDate && item.Source == payment.Source && item.Destination == payment.Destination && item.ClassType == payment.ClassType && item.PassengerCount == payment.PassengerCount)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsUsed, true));
        if (updatedPayments == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public Task<Payment?> GetPaymentAsync(string paymentId, int userId)
    {
        return _context.Payments.FirstOrDefaultAsync(payment =>
            payment.PaymentId == paymentId && payment.UserId == userId);
    }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }
}