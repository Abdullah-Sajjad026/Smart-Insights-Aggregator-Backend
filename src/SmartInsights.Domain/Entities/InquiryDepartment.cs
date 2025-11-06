
namespace SmartInsights.Domain.Entities;

public class InquiryDepartment
{
    public Guid InquiryId { get; set; }
    public Guid DepartmentId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
