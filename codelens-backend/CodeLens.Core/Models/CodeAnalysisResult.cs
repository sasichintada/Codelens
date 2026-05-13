using System;
using System.Collections.Generic;

namespace CodeLens.Core.Models;

public class CodeAnalysisResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalCode { get; set; } = string.Empty;
    public string OptimizedCode { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<CodeIssue> Errors { get; set; } = new();
    public List<CodeIssue> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public ComplexityInfo Complexity { get; set; } = new();
    public int QualityScore { get; set; }
    public List<string> SecurityIssues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public double ProcessingTimeMs { get; set; }
    public bool UsedAI { get; set; } = true;
}

public class CodeIssue
{
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Severity { get; set; } = "error";
    public string Category { get; set; } = "syntax";
    public string Suggestion { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
}

public class ComplexityInfo
{
    public string TimeComplexity { get; set; } = "O(n)";
    public string SpaceComplexity { get; set; } = "O(1)";
    public int CyclomaticComplexity { get; set; }
    public int LinesOfCode { get; set; }
    public int NumberOfFunctions { get; set; }
    public int MaxNestingDepth { get; set; }
    public Dictionary<string, string> FunctionComplexities { get; set; } = new();
}