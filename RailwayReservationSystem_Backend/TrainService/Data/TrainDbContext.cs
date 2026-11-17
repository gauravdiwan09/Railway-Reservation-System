using Microsoft.EntityFrameworkCore;
using TrainService.Models;

namespace TrainService.Data;

public class TrainDbContext : DbContext
{
    public TrainDbContext(DbContextOptions<TrainDbContext> options)
        : base(options)
    {
    }

    public DbSet<Train> Trains { get; set; }

    public DbSet<Schedule> Schedules { get; set; }

    public DbSet<Seat> Seats { get; set; }

    public DbSet<Fare> Fares { get; set; }

    public DbSet<SeatAvailability> SeatAvailabilities { get; set; }

    public DbSet<RouteStation> RouteStations { get; set; }

    public DbSet<SeatReservation> SeatReservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Train
        modelBuilder.Entity<Train>(entity =>
        {
            entity.HasKey(t => t.TrainId);

            entity.Property(t => t.TrainNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(t => t.TrainNumber)
                .IsUnique();

            entity.Property(t => t.TrainName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(t => t.Source)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(t => t.Destination)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(t => t.TotalSeats)
                .IsRequired();

            entity.Property(t => t.TotalDistanceKm)
                .HasPrecision(10, 2)
                .IsRequired();
        });

        modelBuilder.Entity<RouteStation>(entity =>
        {
            entity.HasKey(station => station.RouteStationId);

            entity.Property(station => station.StationName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(station => station.DistanceFromOriginKm)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.HasIndex(station => new { station.TrainId, station.Sequence })
                .IsUnique();

            entity.HasOne(station => station.Train)
                .WithMany(train => train.RouteStations)
                .HasForeignKey(station => station.TrainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SeatReservation>(entity =>
        {
            entity.HasKey(reservation => reservation.SeatReservationId);

            entity.Property(reservation => reservation.JourneyDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(reservation => reservation.StartDistanceKm)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(reservation => reservation.EndDistanceKm)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.HasIndex(reservation => new { reservation.SeatId, reservation.JourneyDate });

            entity.HasOne<Seat>()
                .WithMany()
                .HasForeignKey(reservation => reservation.SeatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Schedule
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(s => s.ScheduleId);

            entity.Property(s => s.DaysOfOperation)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(s => s.Train)
                .WithMany(t => t.Schedules)
                .HasForeignKey(s => s.TrainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seat
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(s => s.SeatId);

            entity.Property(s => s.SeatNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(s => s.ClassType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(s => s.IsAvailable)
                .IsRequired();

            entity.Property(s => s.StartDistanceKm).HasPrecision(10, 2);
            entity.Property(s => s.EndDistanceKm).HasPrecision(10, 2);

            entity.HasOne(s => s.Train)
                .WithMany(t => t.Seats)
                .HasForeignKey(s => s.TrainId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Schedule)
                .WithMany(sch => sch.Seats)
                .HasForeignKey(s => s.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Fare
        modelBuilder.Entity<Fare>(entity =>
        {
            entity.HasKey(f => f.FareId);

            entity.Property(f => f.ClassType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.Amount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.HasOne(f => f.Train)
                .WithMany(t => t.Fares)
                .HasForeignKey(f => f.TrainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        //seatAvailability
        modelBuilder.Entity<SeatAvailability>(entity =>
        {
            entity.HasKey(sa => sa.SeatAvailabilityId);

            entity.Property(sa => sa.JourneyDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(sa => sa.IsAvailable)
                .IsRequired();

            entity.HasOne(sa => sa.Seat)
                .WithMany()
                .HasForeignKey(sa => sa.SeatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(sa => new
            {
                sa.SeatId,
                sa.JourneyDate
            })
            .IsUnique();
        });
    }
}