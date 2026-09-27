using HotelBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<HotelManager> HotelManagers => Set<HotelManager>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationRoom> ReservationRooms => Set<ReservationRoom>();
    public DbSet<RoomHold> RoomHolds => Set<RoomHold>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReservationRoom>()
            .HasKey(x => new { x.ReservationId, x.RoomId });

        modelBuilder.Entity<HotelManager>()
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<HotelManager>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hotel>()
            .HasOne(x => x.Manager)
            .WithMany(x => x.Hotels)
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoomType>()
            .HasOne(x => x.Hotel)
            .WithMany(x => x.RoomTypes)
            .HasForeignKey(x => x.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasOne(x => x.RoomType)
            .WithMany(x => x.Rooms)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Reservation>()
            .HasOne(x => x.Guest)
            .WithMany()
            .HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(x => x.Hotel)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReservationRoom>()
            .HasOne(x => x.Reservation)
            .WithMany(x => x.ReservationRooms)
            .HasForeignKey(x => x.ReservationId);

        modelBuilder.Entity<ReservationRoom>()
            .HasOne(x => x.Room)
            .WithMany(x => x.ReservationRooms)
            .HasForeignKey(x => x.RoomId);

        modelBuilder.Entity<RoomHold>()
            .HasOne(x => x.Room)
            .WithMany(x => x.RoomHolds)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoomHold>()
            .HasOne(x => x.HeldByUser)
            .WithMany()
            .HasForeignKey(x => x.HeldByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Reservation)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CancellationPolicy>()
            .HasOne(x => x.Hotel)
            .WithMany()
            .HasForeignKey(x => x.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payout>()
            .HasOne(x => x.Manager)
            .WithMany()
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<Reservation>()
            .HasIndex(x => x.BookingReference)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasIndex(x => x.RoomNumber);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<RoomType>()
            .Property(x => x.BaseRate)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RatePlan>()
            .Property(x => x.RateOverride)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Reservation>()
            .Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payout>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CancellationPolicy>()
            .Property(x => x.RefundPercentage)
            .HasPrecision(5, 2);
    }
}