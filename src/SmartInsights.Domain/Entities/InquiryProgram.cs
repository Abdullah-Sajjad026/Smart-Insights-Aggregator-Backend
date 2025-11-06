
namespace SmartInsights.Domain.Entities;

public class InquiryProgram
{
    public Guid InquiryId { get; set; }
    public Guid ProgramId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Program Program { get; set; } = null!;
}
