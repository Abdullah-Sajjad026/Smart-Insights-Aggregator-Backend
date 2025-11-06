namespace SmartInsights.Application.DTOs.Inquiries;

public class UpdateInquiryRequest
{
    public string? Body { get; set; }
    public List<Guid>? DepartmentIds { get; set; }
    public List<Guid>? ProgramIds { get; set; }
    public List<Guid>? SemesterIds { get; set; }
}
