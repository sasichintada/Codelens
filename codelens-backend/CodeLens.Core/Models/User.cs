using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeLens.Core.Models;

public class User : IdentityUser
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property for user's analyses
    public ICollection<AnalysisHistory> Analyses { get; set; } = new List<AnalysisHistory>();
    
    // Navigation property for user's sessions
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}