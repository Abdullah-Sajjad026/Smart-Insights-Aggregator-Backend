namespace SmartInsights.Application.DTOs.Inputs;

public class UpdateInputRequest
{
    public string? Body { get; set; }
    public string? Status { get; set; }
    public Guid? TopicId { get; set; }
    public string? Sentiment { get; set; }
    public string? Tone { get; set; }
}
