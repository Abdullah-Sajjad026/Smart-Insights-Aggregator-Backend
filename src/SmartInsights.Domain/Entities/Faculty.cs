using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Faculty : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}
