using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class TopicUpdate : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public string Message { get; set; } = string.Empty;
    public TopicStatus? NewStatus { get; set; } // Null if it's just a message without status change
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Topic Topic { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
