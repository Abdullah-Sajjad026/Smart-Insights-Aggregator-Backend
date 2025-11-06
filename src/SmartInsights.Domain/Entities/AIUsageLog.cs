using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

/// <summary>
/// Tracks AI API usage and costs for monitoring and budgeting
/// </summary>
public class AIUsageLog : BaseEntity
{
    public Guid Id { get; set; }
    public string Operation { get; set; } = string.Empty; // input_analysis, topic_generation, etc.
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public double Cost { get; set; } // In USD
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; } // Optional JSON for additional context
}
