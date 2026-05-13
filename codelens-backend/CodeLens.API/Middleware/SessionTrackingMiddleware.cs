using CodeLens.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace CodeLens.API.Middleware;

public class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTrackingMiddleware> _logger;

    public SessionTrackingMiddleware(RequestDelegate next, ILogger<SessionTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Only validate if both sessionId and userId are present
        if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(userId))
        {
            try
            {
                // Validate session
                var isValid = await sessionService.ValidateSessionAsync(sessionId, userId);
                
                if (!isValid)
                {
                    _logger.LogWarning($"Session {sessionId} is invalid for user {userId}");
                    // Don't block the request, just log it
                    // Let the controller handle authorization
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating session {sessionId}");
            }
        }

        await _next(context);
    }
}