
using SmartInsights.Domain.Enums;
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Input : BaseEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public InputType Type { get; set; } // GENERAL or INQUIRY_LINKED
    public InputStatus Status { get; set; } = InputStatus.Pending;

    // Foreign keys
    public Guid UserId { get; set; }
    public Guid? InquiryId { get; set; } // Null for GENERAL inputs
    public Guid? TopicId { get; set; } // For GENERAL inputs after classification
    public Guid? ThemeId { get; set; } // For GENERAL inputs

    // AI Analysis Results
    public Sentiment? Sentiment { get; set; }
    public Tone? Tone { get; set; }

    // Quality Metrics (0.0 to 1.0)
    public double? UrgencyPct { get; set; }
    public double? ImportancePct { get; set; }
    public double? ClarityPct { get; set; }
    public double? QualityPct { get; set; }
    public double? HelpfulnessPct { get; set; }

    // Derived metrics
    public double? Score { get; set; } // Calculated from quality metrics
    public int? Severity { get; set; } // 1=LOW, 2=MEDIUM, 3=HIGH

    public bool RevealRequested { get; set; } = false;
    public bool? RevealApproved { get; set; } // null=pending, true=approved, false=denied

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Inquiry? Inquiry { get; set; }
    public Topic? Topic { get; set; }
    public Theme? Theme { get; set; }
    public ICollection<InputReply> Replies { get; set; } = new List<InputReply>();

    // Helper methods
    public void CalculateScore()
    {
        if (UrgencyPct.HasValue && ImportancePct.HasValue &&
            ClarityPct.HasValue && QualityPct.HasValue && HelpfulnessPct.HasValue)
        {
            Score = (UrgencyPct.Value + ImportancePct.Value + ClarityPct.Value +
                     QualityPct.Value + HelpfulnessPct.Value) / 5.0;

            // Calculate severity
            if (Score >= 0.75) Severity = 3; // HIGH
            else if (Score >= 0.5) Severity = 2; // MEDIUM
            else Severity = 1; // LOW
        }
    }

    public bool IsAnonymous => !RevealApproved.HasValue || RevealApproved.Value == false;
}
