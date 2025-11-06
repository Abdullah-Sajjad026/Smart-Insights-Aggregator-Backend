namespace SmartInsights.Application.DTOs.Inputs;

public class CreateInputRequest
{
    public string Body { get; set; } = string.Empty;
    public Guid? InquiryId { get; set; } // Null for general feedback
    public Guid? UserId { get; set; } // For logged-in users, null for anonymous
}
