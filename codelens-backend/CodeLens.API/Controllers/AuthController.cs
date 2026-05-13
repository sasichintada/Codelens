using CodeLens.API.Data;
using CodeLens.Core.Models;
using CodeLens.Services.Services;
using CodeLens.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace CodeLens.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly ISessionService _sessionService;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        ISessionService sessionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
        _sessionService = sessionService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName?.Trim() ?? "",
                LastName = model.LastName?.Trim() ?? "",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add to default role
                await _userManager.AddToRoleAsync(user, "User");
                
                _logger.LogInformation($"User registered successfully: {user.Email}");
                
                return Ok(new { 
                    message = "Registration successful", 
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName
                });
            }

            // Return specific password errors
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", model.Email);
            return StatusCode(500, new { error = "Registration failed. Please try again." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User not found - {model.Email}");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Login failed: Inactive account - {model.Email}");
                return Unauthorized(new { message = "Account is deactivated. Please contact support." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var token = _jwtService.GenerateToken(user);

                // Create session with detailed logging
                UserSession session = null;
                try
                {
                    _logger.LogInformation($"Attempting to create session for user: {user.Email}");
                    session = await _sessionService.CreateSessionAsync(user, HttpContext);
                    _logger.LogInformation($"Session creation completed. SessionId: {session?.SessionId}");
                    
                    // Set the session header if session was created
                    if (session != null)
                    {
                        Response.Headers.Append("X-Session-Id", session.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CRITICAL: Session creation failed with exception");
                    // Still return success - token is valid even if session fails
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Return response with or without session
                return Ok(new AuthResponse
                {
                    Token = token,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Expiration = DateTime.Now.AddHours(2),
                    SessionId = session?.SessionId ?? Guid.NewGuid().ToString() // Fallback if session is null
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Login failed: Account locked out - {model.Email}");
                return Unauthorized(new { message = "Account is locked. Please try again later." });
            }

            _logger.LogWarning($"Login failed: Invalid password - {model.Email}");
            return Unauthorized(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", model.Email);
            return StatusCode(500, new { error = "Login failed. Please try again." });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                // Log logout activity before ending session
                await _sessionService.LogActivityAsync(sessionId, "Logout", "User logged out manually", "/api/auth/logout");
                await _sessionService.EndSessionAsync(sessionId);
            }
            
            await _signInManager.SignOutAsync();
            
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { error = "Logout failed" });
        }
    }

    [HttpGet("session/validate")]
    [Authorize]
    public async Task<IActionResult> ValidateSession()
    {
        try
        {
            var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
            {
                return Ok(new { valid = false, message = "No session found" });
            }

            var isValid = await _sessionService.ValidateSessionAsync(sessionId, userId);
            
            return Ok(new { valid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session validation error");
            return Ok(new { valid = false, error = "Session validation failed" });
        }
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var sessions = await _sessionService.GetUserActiveSessionsAsync(userId);
            
            return Ok(sessions.Select(s => new
            {
                sessionId = s.SessionId,
                deviceInfo = s.DeviceInfo,
                ipAddress = s.IpAddress,
                createdAt = s.CreatedAt,
                lastActivityAt = s.LastActivityAt,
                expiresAt = s.ExpiresAt,
                activityCount = s.ActivityCount
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            return StatusCode(500, new { error = "Failed to get sessions" });
        }
    }

    [HttpGet("sessions/{sessionId}/activities")]
    [Authorize]
    public async Task<IActionResult> GetSessionActivities(string sessionId, [FromQuery] int count = 50)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || session.UserId != userId)
                return NotFound();

            var activities = await _sessionService.GetSessionActivitiesAsync(sessionId, count);
            
            return Ok(activities.Select(a => new
            {
                activityType = a.ActivityType,
                description = a.Description,
                timestamp = a.Timestamp,
                endpoint = a.Endpoint
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session activities");
            return StatusCode(500, new { error = "Failed to get activities" });
        }
    }

    [HttpPost("sessions/{sessionId}/end")]
    [Authorize]
    public async Task<IActionResult> EndSession(string sessionId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || session.UserId != userId)
                return NotFound();

            await _sessionService.LogActivityAsync(sessionId, "Session Terminated", "Session ended by user", $"/api/auth/sessions/{sessionId}/end");
            await _sessionService.EndSessionAsync(sessionId);
            
            return Ok(new { message = "Session ended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session");
            return StatusCode(500, new { error = "Failed to end session" });
        }
    }

    [HttpPost("sessions/end-others")]
    [Authorize]
    public async Task<IActionResult> EndOtherSessions()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentSessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
            
            if (userId == null)
                return Unauthorized();

            await _sessionService.EndAllUserSessionsAsync(userId, currentSessionId);
            
            return Ok(new { message = "Other sessions ended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending other sessions");
            return StatusCode(500, new { error = "Failed to end other sessions" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "Not authenticated" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                isActive = user.IsActive,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "Failed to get user information" });
        }
    }

    [HttpGet("debug-sessions")]
    [Authorize]
    public async Task<IActionResult> DebugSessions()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            // Check all sessions in database for this user
            var allSessions = await _sessionService.GetUserActiveSessionsAsync(userId);
            
            return Ok(new
            {
                userId = userId,
                totalSessions = allSessions.Count,
                activeSessions = allSessions.Count(s => s.IsActive),
                sessions = allSessions.Select(s => new
                {
                    s.SessionId,
                    s.IsActive,
                    s.CreatedAt,
                    s.LastActivityAt,
                    s.ExpiresAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug sessions error");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}