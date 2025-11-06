
namespace SmartInsights.Domain.Entities;

public class InquirySemester
{
    public Guid InquiryId { get; set; }
    public Guid SemesterId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
}
