using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Passenger> Passengers { get; set; }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Booking → Passenger (1:N)
        modelBuilder.Entity<Booking>()
            .HasMany(b => b.Passengers)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // PNR must be unique
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.PNR)
            .IsUnique();

        // Configure decimal precision
        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalFare)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Booking>()
            .Property(b => b.PaymentId)
            .HasMaxLength(32)
            .IsRequired();

        modelBuilder.Entity<Booking>().Property(b => b.Source).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Booking>().Property(b => b.Destination).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Booking>().Property(b => b.StartDistanceKm).HasPrecision(10, 2);
        modelBuilder.Entity<Booking>().Property(b => b.EndDistanceKm).HasPrecision(10, 2);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.PaymentId);
            entity.Property(payment => payment.Amount).HasPrecision(10, 2);
            entity.Property(payment => payment.JourneyDate).HasColumnType("date");
            entity.Property(payment => payment.Source).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.Destination).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.ClassType).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.Provider).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.ProviderReference).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.RazorpayPaymentId).HasMaxLength(100);
            entity.Property(payment => payment.Status).HasMaxLength(30).IsRequired();
        });
    }
}