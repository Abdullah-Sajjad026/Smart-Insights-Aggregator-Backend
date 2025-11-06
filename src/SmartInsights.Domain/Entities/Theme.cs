
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class Theme : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Predefined themes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
}
