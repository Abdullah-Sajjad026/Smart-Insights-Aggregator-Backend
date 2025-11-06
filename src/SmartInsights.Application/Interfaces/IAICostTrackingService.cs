namespace SmartInsights.Application.Interfaces;

/// <summary>
/// Service for tracking AI API costs and usage
/// </summary>
public interface IAICostTrackingService
{
    /// <summary>
    /// Log an AI request with token usage
    /// </summary>
    Task LogRequestAsync(string operation, int promptTokens, int completionTokens, double cost);

    /// <summary>
    /// Get total cost for a date range
    /// </summary>
    Task<double> GetTotalCostAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get usage statistics
    /// </summary>
    Task<AIUsageStats> GetUsageStatsAsync(DateTime startDate, DateTime endDate);
}

public class AIUsageStats
{
    public int TotalRequests { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalCompletionTokens { get; set; }
    public double TotalCost { get; set; }
    public Dictionary<string, int> RequestsByOperation { get; set; } = new();
    public double AverageCostPerRequest { get; set; }
}
