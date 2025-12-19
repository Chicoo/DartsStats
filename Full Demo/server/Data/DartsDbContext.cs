using Microsoft.EntityFrameworkCore;
using DartsStats.Api.Entities;

namespace DartsStats.Api.Data;

public class DartsDbContext : DbContext
{
    public DartsDbContext(DbContextOptions<DartsDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<MatchEntity> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Player entity
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.ToTable("Players");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AvgPoints).HasColumnType("decimal(5,2)");
            entity.Property(e => e.AvgLegDarts).HasColumnType("decimal(5,2)");
            entity.Property(e => e.CheckoutPercentage).HasColumnType("decimal(5,2)");
        });

        // Configure Match entity
        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.ToTable("Matches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MatchDate).IsRequired();
            entity.Property(e => e.Season).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Round).IsRequired().HasMaxLength(50);
            
            // Configure relationships
            entity.HasOne(e => e.Player1)
                  .WithMany()
                  .HasForeignKey(e => e.Player1Id)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Player2)
                  .WithMany()
                  .HasForeignKey(e => e.Player2Id)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
