namespace SmartInsights.Application.DTOs.Inputs;

public class InputFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Type { get; set; } // "General" or "InquiryLinked"
    public Guid? InquiryId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Sentiment { get; set; }
    public double? MinQuality { get; set; } // 0.0 to 1.0
    public int? Severity { get; set; } // 1, 2, or 3
    public string? Status { get; set; }
    public string? SortBy { get; set; } = "createdAt"; // "createdAt", "score", "severity"
    public string? SortOrder { get; set; } = "desc"; // "asc" or "desc"
    public string? Search { get; set; }
}
