namespace SmartInsights.Application.DTOs.Common;

/// <summary>
/// DTO for AI-generated executive summaries
/// Used by both Inquiries and Topics
/// </summary>
public class ExecutiveSummaryDto
{
    public List<string> Topics { get; set; } = new();
    public Dictionary<string, string> ExecutiveSummaryData { get; set; } = new();
    public List<SuggestedActionDto> SuggestedPrioritizedActions { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for suggested actions within executive summaries
/// </summary>
public class SuggestedActionDto
{
    public string Action { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Challenges { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public string SupportingReasoning { get; set; } = string.Empty;
}
