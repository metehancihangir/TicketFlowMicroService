using Microsoft.EntityFrameworkCore;
using TicketFlow.EventService.Models;

namespace TicketFlow.EventService.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Venue).IsRequired().HasMaxLength(500);
            // Optimistic concurrency via ConcurrencyStamp
            entity.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        });
    }
}
