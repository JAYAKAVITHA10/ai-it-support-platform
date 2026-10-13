
using ITSupport.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ITSupport.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(t => t.TicketId);

            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).IsRequired();
            entity.Property(t => t.Category).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Priority).HasMaxLength(20).IsRequired();
            entity.Property(t => t.Status).HasMaxLength(30).IsRequired();

            entity.Property(t => t.AISummary).HasColumnType("nvarchar(max)");
            entity.Property(t => t.AIPredictedPriority).HasMaxLength(20);
            entity.Property(t => t.CreatedAt).HasColumnType("datetime2");
            entity.Property(t => t.UpdatedAt).HasColumnType("datetime2");
            entity.Property(t => t.SLADueAt).HasColumnType("datetime2");

            entity.HasIndex(t => new { t.Status, t.Priority })
                .HasDatabaseName("IX_Tickets_Status_Priority");

            entity.HasIndex(t => t.AssignedToUserId)
                .HasDatabaseName("IX_Tickets_AssignedToUserId");
        });
    }
}
