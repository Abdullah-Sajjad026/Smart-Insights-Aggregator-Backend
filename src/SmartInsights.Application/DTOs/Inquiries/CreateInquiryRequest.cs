namespace SmartInsights.Application.DTOs.Inquiries;

public class CreateInquiryRequest
{
    public string Body { get; set; } = string.Empty;
    public List<Guid> DepartmentIds { get; set; } = new();
    public List<Guid> ProgramIds { get; set; } = new();
    public List<Guid> SemesterIds { get; set; } = new();
    public List<Guid> FacultyIds { get; set; } = new();
    public string Status { get; set; } = "Draft"; // Draft or Active
}
