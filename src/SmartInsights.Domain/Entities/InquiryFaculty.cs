namespace SmartInsights.Domain.Entities;

public class InquiryFaculty
{
    public Guid InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public Guid FacultyId { get; set; }
    public Faculty Faculty { get; set; } = null!;
}
