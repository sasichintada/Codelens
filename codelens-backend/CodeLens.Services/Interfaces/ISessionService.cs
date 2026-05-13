using CodeLens.Core.Models;
using Microsoft.AspNetCore.Http;

namespace CodeLens.Services.Interfaces;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(User user, HttpContext httpContext);
    Task<UserSession?> GetSessionAsync(string sessionId);
    Task<bool> ValidateSessionAsync(string sessionId, string userId);
    Task UpdateSessionActivityAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
    Task EndAllUserSessionsAsync(string userId, string? currentSessionId = null);
    Task<List<UserSession>> GetUserActiveSessionsAsync(string userId);
    Task LogActivityAsync(string sessionId, string activityType, string? description = null, string? endpoint = null, string? data = null);
    Task<List<SessionActivity>> GetSessionActivitiesAsync(string sessionId, int count = 50);
}