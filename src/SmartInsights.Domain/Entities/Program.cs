
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Program : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Computer Science"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<InquiryProgram> InquiryPrograms { get; set; } = new List<InquiryProgram>();
}
