
using SmartInsights.Domain.Common;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Domain.Entities;

public class Theme : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Predefined themes
    public ThemeType Type { get; set; } = ThemeType.Other;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
}
