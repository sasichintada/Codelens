using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CodeLens.Core.Models;

public class AnalysisHistory
{
    [Key]
    public int Id { get; set; }
    
    public string? UserId { get; set; }   // nullable
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    public string Language { get; set; } = string.Empty;
    
    public string OriginalCode { get; set; } = string.Empty;
    
    public string OptimizedCode { get; set; } = string.Empty;
    
    public string AnalysisResult { get; set; } = string.Empty;
    
    public int QualityScore { get; set; }
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    
    // Helper methods to serialize/deserialize complex objects
    public void SetErrors(List<string> errors)
    {
        var data = new Dictionary<string, object> { ["errors"] = errors };
        UpdateAnalysisResult(data);
    }
    
    public List<string> GetErrors()
    {
        return GetFromAnalysisResult<List<string>>("errors") ?? new List<string>();
    }
    
    public void SetSuggestions(List<string> suggestions)
    {
        var data = new Dictionary<string, object> { ["suggestions"] = suggestions };
        UpdateAnalysisResult(data);
    }
    
    public List<string> GetSuggestions()
    {
        return GetFromAnalysisResult<List<string>>("suggestions") ?? new List<string>();
    }
    
    public void SetSecurityIssues(List<string> securityIssues)
    {
        var data = new Dictionary<string, object> { ["securityIssues"] = securityIssues };
        UpdateAnalysisResult(data);
    }
    
    public List<string> GetSecurityIssues()
    {
        return GetFromAnalysisResult<List<string>>("securityIssues") ?? new List<string>();
    }
    
    public void SetComplexity(Dictionary<string, string> complexity)
    {
        var data = new Dictionary<string, object> { ["complexity"] = complexity };
        UpdateAnalysisResult(data);
    }
    
    public Dictionary<string, string> GetComplexity()
    {
        return GetFromAnalysisResult<Dictionary<string, string>>("complexity") ?? new Dictionary<string, string>();
    }
    
    private void UpdateAnalysisResult(Dictionary<string, object> newData)
    {
        try
        {
            var currentData = string.IsNullOrEmpty(AnalysisResult) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(AnalysisResult) ?? new Dictionary<string, object>();
            
            foreach (var kvp in newData)
            {
                currentData[kvp.Key] = kvp.Value;
            }
            
            AnalysisResult = JsonSerializer.Serialize(currentData);
        }
        catch
        {
            AnalysisResult = JsonSerializer.Serialize(newData);
        }
    }
    
    private T? GetFromAnalysisResult<T>(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(AnalysisResult))
                return default;
                
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(AnalysisResult);
            if (data != null && data.TryGetValue(key, out var element))
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing {key}: {ex.Message}");
        }
        
        return default;
    }
}