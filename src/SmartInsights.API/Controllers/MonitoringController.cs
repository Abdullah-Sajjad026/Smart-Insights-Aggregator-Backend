using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.API.Controllers;

/// <summary>
/// Admin endpoints for monitoring AI processing, costs, and system health
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class MonitoringController : ControllerBase
{
    private readonly IAICostTrackingService _costTracking;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IAICostTrackingService costTracking,
        ILogger<MonitoringController> logger)
    {
        _costTracking = costTracking;
        _logger = logger;
    }

    /// <summary>
    /// Get AI cost for today
    /// </summary>
    [HttpGet("ai/cost/today")]
    public async Task<IActionResult> GetTodayCost()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var cost = await _costTracking.GetTotalCostAsync(today, tomorrow);

            return Ok(new
            {
                date = today.ToString("yyyy-MM-dd"),
                totalCost = cost,
                currency = "USD"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get today's AI cost");
            return StatusCode(500, new { error = "Failed to retrieve cost data" });
        }
    }

    /// <summary>
    /// Get AI cost for a specific date range
    /// </summary>
    [HttpGet("ai/cost")]
    public async Task<IActionResult> GetCostRange(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var cost = await _costTracking.GetTotalCostAsync(start, end);

            return Ok(new
            {
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd"),
                totalCost = cost,
                currency = "USD",
                daysSpan = (end - start).Days
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI cost range");
            return StatusCode(500, new { error = "Failed to retrieve cost data" });
        }
    }

    /// <summary>
    /// Get detailed AI usage statistics
    /// </summary>
    [HttpGet("ai/usage")]
    public async Task<IActionResult> GetUsageStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _costTracking.GetUsageStatsAsync(start, end);

            return Ok(new
            {
                period = new
                {
                    startDate = start.ToString("yyyy-MM-dd"),
                    endDate = end.ToString("yyyy-MM-dd"),
                    daysSpan = (end - start).Days
                },
                summary = new
                {
                    totalRequests = stats.TotalRequests,
                    totalTokens = stats.TotalPromptTokens + stats.TotalCompletionTokens,
                    promptTokens = stats.TotalPromptTokens,
                    completionTokens = stats.TotalCompletionTokens,
                    totalCost = stats.TotalCost,
                    averageCostPerRequest = stats.AverageCostPerRequest,
                    currency = "USD"
                },
                byOperation = stats.RequestsByOperation.Select(kvp => new
                {
                    operation = kvp.Key,
                    requestCount = kvp.Value
                }).OrderByDescending(x => x.requestCount)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI usage statistics");
            return StatusCode(500, new { error = "Failed to retrieve usage statistics" });
        }
    }

    /// <summary>
    /// Get AI usage statistics for the current month
    /// </summary>
    [HttpGet("ai/usage/month")]
    public async Task<IActionResult> GetMonthlyUsage()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var stats = await _costTracking.GetUsageStatsAsync(startOfMonth, endOfMonth);

            return Ok(new
            {
                month = startOfMonth.ToString("yyyy-MM"),
                summary = new
                {
                    totalRequests = stats.TotalRequests,
                    totalCost = stats.TotalCost,
                    averageDailyCost = stats.TotalCost / DateTime.UtcNow.Day,
                    projectedMonthCost = (stats.TotalCost / DateTime.UtcNow.Day) * DateTime.DaysInMonth(now.Year, now.Month),
                    currency = "USD"
                },
                breakdown = stats.RequestsByOperation.Select(kvp => new
                {
                    operation = kvp.Key,
                    requestCount = kvp.Value
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get monthly AI usage");
            return StatusCode(500, new { error = "Failed to retrieve monthly usage" });
        }
    }

    /// <summary>
    /// Get cost projection based on current usage
    /// </summary>
    [HttpGet("ai/cost/projection")]
    public async Task<IActionResult> GetCostProjection()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var monthCost = await _costTracking.GetTotalCostAsync(startOfMonth, now.AddDays(1));
            var daysElapsed = now.Day;
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            var averageDailyCost = daysElapsed > 0 ? monthCost / daysElapsed : 0;
            var projectedMonthCost = averageDailyCost * daysInMonth;

            return Ok(new
            {
                month = startOfMonth.ToString("yyyy-MM"),
                daysElapsed,
                daysRemaining = daysInMonth - daysElapsed,
                costToDate = monthCost,
                averageDailyCost,
                projectedMonthCost,
                currency = "USD"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost projection");
            return StatusCode(500, new { error = "Failed to calculate projection" });
        }
    }

    /// <summary>
    /// Get system processing statistics
    /// </summary>
    [HttpGet("processing/stats")]
    public async Task<IActionResult> GetProcessingStats(
        [FromServices] IRepository<Domain.Entities.Input> inputRepository)
    {
        try
        {
            var totalInputs = await inputRepository.CountAsync();
            var processedInputs = await inputRepository.CountAsync(i => i.AIProcessedAt != null);
            var pendingInputs = await inputRepository.CountAsync(i => i.Status == Domain.Enums.InputStatus.Pending);

            var today = DateTime.UtcNow.Date;
            var todayInputs = await inputRepository.CountAsync(i => i.CreatedAt >= today);
            var todayProcessed = await inputRepository.CountAsync(i =>
                i.CreatedAt >= today && i.AIProcessedAt != null);

            return Ok(new
            {
                overall = new
                {
                    totalInputs,
                    processedInputs,
                    pendingInputs,
                    processingRate = totalInputs > 0 ? (double)processedInputs / totalInputs * 100 : 0
                },
                today = new
                {
                    totalInputs = todayInputs,
                    processedInputs = todayProcessed,
                    pendingInputs = todayInputs - todayProcessed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get processing statistics");
            return StatusCode(500, new { error = "Failed to retrieve processing stats" });
        }
    }
}
