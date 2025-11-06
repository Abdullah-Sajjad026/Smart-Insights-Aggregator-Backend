using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Infrastructure.Data;

namespace SmartInsights.Infrastructure.Services;

public class AICostTrackingService : IAICostTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AICostTrackingService> _logger;

    public AICostTrackingService(
        ApplicationDbContext context,
        ILogger<AICostTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogRequestAsync(string operation, int promptTokens, int completionTokens, double cost)
    {
        try
        {
            var log = new AIUsageLog
            {
                Id = Guid.NewGuid(),
                Operation = operation,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                Cost = cost,
                CreatedAt = DateTime.UtcNow
            };

            _context.AIUsageLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "AI cost tracked: {Operation} - ${Cost:F4} ({Total} tokens)",
                operation,
                cost,
                log.TotalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log AI cost for operation: {Operation}", operation);
            // Don't throw - cost tracking failure shouldn't break the application
        }
    }

    public async Task<double> GetTotalCostAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var totalCost = await _context.AIUsageLogs
                .Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate)
                .SumAsync(log => log.Cost);

            return totalCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total cost for period {Start} to {End}", startDate, endDate);
            return 0;
        }
    }

    public async Task<AIUsageStats> GetUsageStatsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var logs = await _context.AIUsageLogs
                .Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate)
                .ToListAsync();

            if (!logs.Any())
            {
                return new AIUsageStats
                {
                    TotalRequests = 0,
                    TotalPromptTokens = 0,
                    TotalCompletionTokens = 0,
                    TotalCost = 0,
                    RequestsByOperation = new Dictionary<string, int>(),
                    AverageCostPerRequest = 0
                };
            }

            var stats = new AIUsageStats
            {
                TotalRequests = logs.Count,
                TotalPromptTokens = logs.Sum(l => l.PromptTokens),
                TotalCompletionTokens = logs.Sum(l => l.CompletionTokens),
                TotalCost = logs.Sum(l => l.Cost),
                RequestsByOperation = logs
                    .GroupBy(l => l.Operation)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageCostPerRequest = logs.Average(l => l.Cost)
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats for period {Start} to {End}", startDate, endDate);

            return new AIUsageStats
            {
                TotalRequests = 0,
                TotalPromptTokens = 0,
                TotalCompletionTokens = 0,
                TotalCost = 0,
                RequestsByOperation = new Dictionary<string, int>(),
                AverageCostPerRequest = 0
            };
        }
    }
}
