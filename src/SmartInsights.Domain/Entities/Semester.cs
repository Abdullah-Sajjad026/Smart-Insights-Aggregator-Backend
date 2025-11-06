
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Semester : BaseEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty; // "1", "2", "3"... "8"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<InquirySemester> InquirySemesters { get; set; } = new List<InquirySemester>();
}
