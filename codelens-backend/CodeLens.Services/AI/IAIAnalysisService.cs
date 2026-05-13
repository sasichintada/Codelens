using CodeLens.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeLens.Services.AI;

public interface IAIAnalysisService
{
    Task<CodeAnalysisResult> AnalyzeCodeAsync(CodeAnalysisRequest request);
    Task<string> OptimizeCodeAsync(string code, string language, string level);
    Task<List<CodeIssue>> DetectErrorsAsync(string code, string language);
    Task<List<string>> GetSuggestionsAsync(string code, string language);
    Task<ComplexityInfo> AnalyzeComplexityAsync(string code, string language);
    Task<List<string>> SecurityAuditAsync(string code, string language);
}