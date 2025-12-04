
using SmartInsights.Domain.Common;
using System.Text.Json;

namespace SmartInsights.Domain.Entities;

public enum TopicStatus
{
    Submitted = 0,      // Initial state when topic is created
    UnderReview = 1,    // Admin is reviewing the feedback
    InProgress = 2,     // Action is being taken
    Completed = 3,      // Issue has been resolved
    Planned = 4,        // Planned for future implementation
    Rejected = 5        // Won't be addressed (with reason in updates)
}

public class Topic : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // AI-generated (max 5 words)
    public Guid? DepartmentId { get; set; }
    public bool IsArchived { get; set; } = false;

    // Status tracking
    public TopicStatus Status { get; set; } = TopicStatus.Submitted;
    public DateTime? StatusUpdatedAt { get; set; }

    // AI-generated summary (stored as JSON)
    public string? Summary { get; set; } // ExecutiveSummary JSON
    public DateTime? SummaryGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
    public ICollection<TopicUpdate> Updates { get; set; } = new List<TopicUpdate>();

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

    public void UpdateStatus(TopicStatus newStatus)
    {
        Status = newStatus;
        StatusUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
