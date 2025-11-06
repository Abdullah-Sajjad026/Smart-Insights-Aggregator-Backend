namespace SmartInsights.Application.DTOs.Inputs;

public class InputDto
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Sentiment { get; set; }
    public string? Tone { get; set; }
    public QualityMetrics? Metrics { get; set; }
    public InputUserInfo User { get; set; } = new();
    public InquiryBasicInfo? Inquiry { get; set; }
    public TopicBasicInfo? Topic { get; set; }
    public ThemeBasicInfo? Theme { get; set; }
    public int ReplyCount { get; set; }
    public bool RevealRequested { get; set; }
    public bool? RevealApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QualityMetrics
{
    public double Urgency { get; set; }
    public double Importance { get; set; }
    public double Clarity { get; set; }
    public double Quality { get; set; }
    public double Helpfulness { get; set; }
    public double Score { get; set; }
    public int Severity { get; set; } // 1=LOW, 2=MEDIUM, 3=HIGH
}

public class InputUserInfo
{
    public string? Department { get; set; }
    public string? Program { get; set; }
    public string? Semester { get; set; }
    public bool IsAnonymous { get; set; }
    public string? FirstName { get; set; } // Only if revealed
    public string? LastName { get; set; } // Only if revealed
    public string? Email { get; set; } // Only if revealed
}

public class InquiryBasicInfo
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class TopicBasicInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ThemeBasicInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
