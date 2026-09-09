using Microsoft.EntityFrameworkCore;
using TicketFlow.ReservationService.Models;

namespace TicketFlow.ReservationService.Data;

public class ReservationDbContext : DbContext
{
    public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options) { }

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.EventId);
            entity.Property(r => r.Status).HasConversion<string>();
            entity.Property(r => r.PaymentStatus).HasConversion<string>();
        });
    }
}
