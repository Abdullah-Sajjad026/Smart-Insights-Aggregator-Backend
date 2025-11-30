using SmartInsights.Application.DTOs.Users;
using System.Text.Json.Serialization;

namespace SmartInsights.Application.DTOs.Inputs;

public class InputReplyDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty; // "Admin" or "Student"
    public ReplyUserInfo User { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ReplyUserInfo
{
    public Guid Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastName { get; set; }
}
