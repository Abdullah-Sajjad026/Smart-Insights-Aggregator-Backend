using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Interfaces;

public interface IAIService
{
    /// <summary>
    /// Analyze a single input for sentiment, tone, quality metrics, and theme
    /// </summary>
    Task<InputAnalysisResult> AnalyzeInputAsync(string body, InputType type);

    /// <summary>
    /// Generate or find matching topic for general feedback
    /// </summary>
    Task<Topic> GenerateOrFindTopicAsync(string body, Guid? departmentId);

    /// <summary>
    /// Generate executive summary for an inquiry based on all its inputs
    /// </summary>
    Task<ExecutiveSummary> GenerateInquirySummaryAsync(Guid inquiryId, List<Input> inputs);

    /// <summary>
    /// Generate executive summary for a topic based on all its inputs
    /// </summary>
    Task<ExecutiveSummary> GenerateTopicSummaryAsync(Guid topicId, List<Input> inputs, bool bypassCache = false);
}

public class InputAnalysisResult
{
    public Sentiment Sentiment { get; set; }
    public Tone Tone { get; set; }
    public double Urgency { get; set; } // 0.0 to 1.0
    public double Importance { get; set; }
    public double Clarity { get; set; }
    public double Quality { get; set; }
    public double Helpfulness { get; set; }
    public double Score { get; set; } // Average of all metrics
    public int Severity { get; set; } // 1=LOW, 2=MEDIUM, 3=HIGH
    public string ExtractedTheme { get; set; } = string.Empty; // For general inputs
}
