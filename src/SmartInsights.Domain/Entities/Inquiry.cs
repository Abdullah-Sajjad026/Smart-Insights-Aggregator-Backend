
using SmartInsights.Domain.Enums;
using System.Text.Json;
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Inquiry : BaseEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty; // The question/prompt
    public InquiryStatus Status { get; set; } = InquiryStatus.Draft;

    // Creator info
    public Guid CreatedById { get; set; }

    // AI-generated summary (stored as JSON)
    public string? Summary { get; set; } // ExecutiveSummary JSON
    public DateTime? SummaryGeneratedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<InquiryDepartment> InquiryDepartments { get; set; } = new List<InquiryDepartment>();
    public ICollection<InquiryProgram> InquiryPrograms { get; set; } = new List<InquiryProgram>();
    public ICollection<InquirySemester> InquirySemesters { get; set; } = new List<InquirySemester>();

    public ICollection<Input> Inputs { get; set; } = new List<Input>();

    // Helper methods
    public ExecutiveSummary? GetParsedSummary()
    {
        if (string.IsNullOrEmpty(Summary)) return null;
        return JsonSerializer.Deserialize<ExecutiveSummary>(Summary);
    }

    public void SetSummary(ExecutiveSummary summary)
    {
        Summary = JsonSerializer.Serialize(summary);
        SummaryGeneratedAt = DateTime.UtcNow;
    }
}

// DTO for AI Summary
public class ExecutiveSummary
{
    public List<string> Topics { get; set; } = new();
    public Dictionary<string, string> ExecutiveSummaryData { get; set; } = new();
    public List<SuggestedAction> SuggestedPrioritizedActions { get; set; } = new();
}

public class SuggestedAction
{
    public string Action { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Challenges { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public string SupportingReasoning { get; set; } = string.Empty;
}
