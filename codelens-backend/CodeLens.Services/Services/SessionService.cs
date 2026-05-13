using CodeLens.Core.Models;
using CodeLens.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CodeLens.Core.Interfaces;

namespace CodeLens.Services.Services;

public class SessionService : ISessionService
{
    private readonly IAppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        IAppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(User user, HttpContext httpContext)
    {
        try
        {
            _logger.LogInformation($"=== CREATING SESSION FOR USER: {user.Email} ===");
            
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            
            _logger.LogInformation($"IP Address: {ipAddress}");
            _logger.LogInformation($"User Agent: {userAgent}");
            
            var deviceInfo = ParseUserAgent(userAgent);
            _logger.LogInformation($"Device Info: {deviceInfo}");
            
            var session = new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = Guid.NewGuid().ToString(),
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                ActivityCount = 0
            };

            _logger.LogInformation($"Session object created with ID: {session.Id}, SessionId: {session.SessionId}");
            
            _context.UserSessions.Add(session);
            _logger.LogInformation("Session added to context");
            
            var saveResult = await _context.SaveChangesAsync();
            _logger.LogInformation($"SaveChangesAsync result: {saveResult} entities saved");

            // Log the login activity after session is saved
            await LogActivityAsync(session.SessionId, "Login", "User logged in successfully", "/api/auth/login");

            _logger.LogInformation($"✅ Session created successfully for user {user.Email} with session ID: {session.SessionId}");
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating session for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<UserSession?> GetSessionAsync(string sessionId)
    {
        try
        {
            return await _context.UserSessions
                .Include(s => s.Activities)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting session {sessionId}");
            return null;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionId, string userId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                _logger.LogWarning($"Session {sessionId} not found");
                return false;
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning($"Session {sessionId} belongs to different user");
                return false;
            }

            if (!session.IsActive)
            {
                _logger.LogWarning($"Session {sessionId} is inactive");
                return false;
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning($"Session {sessionId} has expired");
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return false;
            }

            // Update last activity
            session.LastActivityAt = DateTime.UtcNow;
            session.ActivityCount++;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating session {sessionId}");
            return false;
        }
    }

    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session != null && session.IsActive)
            {
                session.LastActivityAt = DateTime.UtcNow;
                session.ActivityCount++;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating session activity for {sessionId}");
        }
    }

    public async Task EndSessionAsync(string sessionId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session != null)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                
                await LogActivityAsync(sessionId, "Logout", "User logged out");
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Session {sessionId} ended");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ending session {sessionId}");
        }
    }

    public async Task EndAllUserSessionsAsync(string userId, string? currentSessionId = null)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                if (currentSessionId == null || session.SessionId != currentSessionId)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    
                    await LogActivityAsync(session.SessionId, "Session Terminated", "Session terminated by user from another session");
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"All sessions ended for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error ending all sessions for user {userId}");
        }
    }

    public async Task<List<UserSession>> GetUserActiveSessionsAsync(string userId)
    {
        try
        {
            return await _context.UserSessions
                .Include(s => s.Activities)
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting active sessions for user {userId}");
            return new List<UserSession>();
        }
    }

    public async Task LogActivityAsync(string sessionId, string activityType, string? description = null, string? endpoint = null, string? data = null)
    {
        try
        {
            // First check if the session exists using SessionId
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            
            if (session == null)
            {
                _logger.LogWarning($"Cannot log activity: Session {sessionId} not found");
                return;
            }

            var activity = new SessionActivity
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                ActivityType = activityType,
                Description = description,
                Endpoint = endpoint,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            _context.SessionActivities.Add(activity);
            
            // Update session's last activity
            session.LastActivityAt = DateTime.UtcNow;
            session.ActivityCount++;

            await _context.SaveChangesAsync();
            
            _logger.LogDebug($"Activity logged for session {sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity for session {SessionId}", sessionId);
        }
    }

    public async Task<List<SessionActivity>> GetSessionActivitiesAsync(string sessionId, int count = 50)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new List<SessionActivity>();

            return await _context.SessionActivities
                .Where(a => a.SessionId == session.Id)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting activities for session {sessionId}");
            return new List<SessionActivity>();
        }
    }

    private string ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        var deviceInfo = new System.Text.StringBuilder();

        // Browser detection
        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
            deviceInfo.Append("Chrome ");
        else if (userAgent.Contains("Firefox"))
            deviceInfo.Append("Firefox ");
        else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
            deviceInfo.Append("Safari ");
        else if (userAgent.Contains("Edg"))
            deviceInfo.Append("Edge ");
        else if (userAgent.Contains("MSIE") || userAgent.Contains("Trident"))
            deviceInfo.Append("Internet Explorer ");

        // OS detection
        if (userAgent.Contains("Windows NT 10.0"))
            deviceInfo.Append("on Windows 10");
        else if (userAgent.Contains("Windows NT 11.0"))
            deviceInfo.Append("on Windows 11");
        else if (userAgent.Contains("Mac OS X"))
            deviceInfo.Append("on macOS");
        else if (userAgent.Contains("Linux"))
            deviceInfo.Append("on Linux");
        else if (userAgent.Contains("Android"))
            deviceInfo.Append("on Android");
        else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            deviceInfo.Append("on iOS");

        return deviceInfo.Length > 0 ? deviceInfo.ToString() : userAgent[..Math.Min(50, userAgent.Length)];
    }
}