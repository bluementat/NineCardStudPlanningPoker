using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Data;

public class PlanningPokerDbContext : DbContext
{
    public PlanningPokerDbContext(DbContextOptions<PlanningPokerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Session> Sessions { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.PIN)
                .IsRequired()
                .HasMaxLength(6);
            entity.HasIndex(e => e.PIN)
                .IsUnique();
            entity.Property(e => e.SessionName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });

        // Configure Participant
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participants");
            entity.HasKey(e => e.ParticipantId);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Participants)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Vote
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.ToTable("Votes");
            entity.HasKey(e => e.VoteId);
            entity.Property(e => e.CardValue)
                .IsRequired()
                .HasMaxLength(10);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Votes)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Participant)
                .WithMany(p => p.Votes)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Ensure one vote per participant per session (for current round)
            entity.HasIndex(e => new { e.SessionId, e.ParticipantId, e.VotedAt });
        });
    }
}
