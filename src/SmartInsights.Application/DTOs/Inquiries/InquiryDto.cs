namespace SmartInsights.Application.DTOs.Inquiries;

public class InquiryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public InquiryCreatorInfo CreatedBy { get; set; } = new();
    public List<string> TargetDepartments { get; set; } = new();
    public List<string> TargetPrograms { get; set; } = new();
    public List<string> TargetSemesters { get; set; } = new();
    public List<string> TargetFaculties { get; set; } = new();
    public InquiryStats Stats { get; set; } = new();
    public ExecutiveSummaryDto? AiSummary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class InquiryCreatorInfo
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class InquiryStats
{
    public int TotalResponses { get; set; }
    public double AverageQuality { get; set; }
    public Dictionary<string, int> SentimentBreakdown { get; set; } = new();
    public Dictionary<string, int> SeverityBreakdown { get; set; } = new();
}

public class ExecutiveSummaryDto
{
    public List<string> Topics { get; set; } = new();
    public Dictionary<string, string> ExecutiveSummaryData { get; set; } = new();
    public List<SuggestedActionDto> SuggestedPrioritizedActions { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class SuggestedActionDto
{
    public string Action { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Challenges { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public string SupportingReasoning { get; set; } = string.Empty;
}
