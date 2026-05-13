using CodeLens.Core.Models;
using CodeLens.Core.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeLens.API.Data;

public class AppDbContext : IdentityDbContext<User>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // These are the actual DbSets
    public DbSet<AnalysisHistory> AnalysisHistories { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<SessionActivity> SessionActivities { get; set; }
    
    // Explicit interface implementation
    DbSet<User> IAppDbContext.Users 
    { 
        get => Users; 
        set => Users = value; 
    }
    
    DbSet<UserSession> IAppDbContext.UserSessions 
    { 
        get => UserSessions; 
        set => UserSessions = value; 
    }
    
    DbSet<SessionActivity> IAppDbContext.SessionActivities 
    { 
        get => SessionActivities; 
        set => SessionActivities = value; 
    }
    
    DbSet<AnalysisHistory> IAppDbContext.AnalysisHistories 
    { 
        get => AnalysisHistories; 
        set => AnalysisHistories = value; 
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User - Session relationship
        builder.Entity<UserSession>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session - Activity relationship
        builder.Entity<SessionActivity>()
            .HasOne(a => a.Session)
            .WithMany(s => s.Activities)
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for better performance
        builder.Entity<UserSession>()
            .HasIndex(s => s.SessionId)
            .IsUnique();

        builder.Entity<UserSession>()
            .HasIndex(s => new { s.UserId, s.IsActive });

        builder.Entity<SessionActivity>()
            .HasIndex(a => new { a.SessionId, a.Timestamp });

        // Configure AnalysisHistory
        builder.Entity<AnalysisHistory>()
            .Property(a => a.OriginalCode)
            .HasColumnType("TEXT");
        
        builder.Entity<AnalysisHistory>()
            .Property(a => a.OptimizedCode)
            .HasColumnType("TEXT");
        
        builder.Entity<AnalysisHistory>()
            .Property(a => a.AnalysisResult)
            .HasColumnType("TEXT");
    }
}