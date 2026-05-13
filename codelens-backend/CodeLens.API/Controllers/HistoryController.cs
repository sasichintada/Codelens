using CodeLens.API.Data;
using CodeLens.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeLens.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(AppDbContext context, ILogger<HistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/history/recent?count=20
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentHistory([FromQuery] int count = 20)
    {
        try
        {
            var history = await _context.AnalysisHistories
                .OrderByDescending(h => h.AnalyzedAt)
                .Take(count)
                .ToListAsync();

            // Convert to DTO with parsed JSON
            var result = history.Select(h => new
            {
                h.Id,
                h.OriginalCode,
                h.OptimizedCode,
                h.Language,
                h.QualityScore,
                h.AnalyzedAt,
                Errors = h.GetErrors(),
                Suggestions = h.GetSuggestions(),
                SecurityIssues = h.GetSecurityIssues(),
                Complexity = h.GetComplexity()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent history");
            return StatusCode(500, new { error = "Failed to retrieve history" });
        }
    }

    // GET: api/history/language/python?count=20
    [HttpGet("language/{language}")]
    public async Task<IActionResult> GetHistoryByLanguage(string language, [FromQuery] int count = 20)
    {
        try
        {
            var history = await _context.AnalysisHistories
                .Where(h => h.Language.ToLower() == language.ToLower())
                .OrderByDescending(h => h.AnalyzedAt)
                .Take(count)
                .ToListAsync();

            var result = history.Select(h => new
            {
                h.Id,
                h.OriginalCode,
                h.OptimizedCode,
                h.Language,
                h.QualityScore,
                h.AnalyzedAt,
                Errors = h.GetErrors(),
                Suggestions = h.GetSuggestions(),
                SecurityIssues = h.GetSecurityIssues(),
                Complexity = h.GetComplexity()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting history for language: {language}");
            return StatusCode(500, new { error = "Failed to retrieve history" });
        }
    }

    // GET: api/history/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetHistoryById(int id)
    {
        try
        {
            var history = await _context.AnalysisHistories.FindAsync(id);
            if (history == null)
                return NotFound();

            return Ok(new
            {
                history.Id,
                history.OriginalCode,
                history.OptimizedCode,
                history.Language,
                history.QualityScore,
                history.AnalyzedAt,
                Errors = history.GetErrors(),
                Suggestions = history.GetSuggestions(),
                SecurityIssues = history.GetSecurityIssues(),
                Complexity = history.GetComplexity()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting history by ID: {id}");
            return StatusCode(500, new { error = "Failed to retrieve history" });
        }
    }

    // DELETE: api/history/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        try
        {
            var history = await _context.AnalysisHistories.FindAsync(id);
            if (history == null)
                return NotFound();

            _context.AnalysisHistories.Remove(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted history #{id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting history #{id}");
            return StatusCode(500, new { error = "Failed to delete history" });
        }
    }

    // DELETE: api/history/clear
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAllHistory()
    {
        try
        {
            _context.AnalysisHistories.RemoveRange(_context.AnalysisHistories);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleared all history");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all history");
            return StatusCode(500, new { error = "Failed to clear history" });
        }
    }

    // GET: api/history/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var totalCount = await _context.AnalysisHistories.CountAsync();
            var byLanguage = await _context.AnalysisHistories
                .GroupBy(h => h.Language)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .ToListAsync();

            var avgScore = await _context.AnalysisHistories
                .AverageAsync(h => (double?)h.QualityScore) ?? 0;

            var lastUpdated = await _context.AnalysisHistories
                .MaxAsync(h => (DateTime?)h.AnalyzedAt);

            return Ok(new
            {
                totalEntries = totalCount,
                averageScore = Math.Round(avgScore, 1),
                byLanguage = byLanguage,
                lastUpdated = lastUpdated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, new { error = "Failed to retrieve stats" });
        }
    }
}