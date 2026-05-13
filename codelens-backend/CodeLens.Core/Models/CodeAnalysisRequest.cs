using System;

namespace CodeLens.Core.Models;

public class CodeAnalysisRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public AnalysisOptions Options { get; set; } = new();
}

public class AnalysisOptions
{
    public bool FixErrors { get; set; } = true;
    public bool OptimizeCode { get; set; } = true;
    public bool AddComments { get; set; } = false;
    public bool SecurityCheck { get; set; } = true;
    public string OptimizationLevel { get; set; } = "balanced";
}