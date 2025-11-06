
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class InputReply : BaseEntity
{
    public Guid Id { get; set; }
    public Guid InputId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Input Input { get; set; } = null!;
    public User User { get; set; } = null!;
}
