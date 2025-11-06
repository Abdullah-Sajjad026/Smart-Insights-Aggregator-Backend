
using SmartInsights.Domain.Common;
using System.Text.Json;

namespace SmartInsights.Domain.Entities;

public class Topic : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // AI-generated (max 5 words)
    public Guid? DepartmentId { get; set; }

    // AI-generated summary (stored as JSON)
    public string? Summary { get; set; } // ExecutiveSummary JSON
    public DateTime? SummaryGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Department? Department { get; set; }
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
