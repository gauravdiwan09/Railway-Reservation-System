using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {

    }

    // FIX: Expose your tables so you can run queries like _context.Users.Add(...)
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.RoleName)
            .HasMaxLength(50)
            .IsRequired();
        });

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                RoleId = 1,
                RoleName = "Passenger"
            },
            new Role
            {
                RoleId = 2,
                RoleName = "Admin"
            }
        );

        // user configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

            entity.HasIndex(u => u.Email)
            .IsUnique();

            entity.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

            entity.Property(u => u.Phone)
            .HasMaxLength(15);

            entity.Property(u => u.CreatedAt)
            .IsRequired();

            //role (1) -> users (many)
            entity.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        });
    }
}