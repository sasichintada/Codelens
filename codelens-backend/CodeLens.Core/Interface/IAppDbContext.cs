using CodeLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace CodeLens.Core.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<UserSession> UserSessions { get; set; }
    DbSet<SessionActivity> SessionActivities { get; set; }
    DbSet<AnalysisHistory> AnalysisHistories { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}