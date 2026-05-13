using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeLens.Core.Models;

public class UserSession
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [Required]
    public string? DeviceInfo { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastActivityAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int ActivityCount { get; set; } = 0;
    
    public virtual ICollection<SessionActivity> Activities { get; set; } = new List<SessionActivity>();
}

public class SessionActivity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    [ForeignKey("SessionId")]
    public virtual UserSession? Session { get; set; }
    
    [Required]
    public string ActivityType { get; set; } = string.Empty; // Login, Logout, Analysis, ViewHistory, etc.
    
    public string? Description { get; set; }
    
    public string? Endpoint { get; set; }
    
    public string? Data { get; set; } // JSON data of the activity
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}