
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Department : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? FacultyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Faculty? Faculty { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<InquiryDepartment> InquiryDepartments { get; set; } = new List<InquiryDepartment>();
}
