
namespace SmartInsights.Domain.Enums;
public enum InputStatus
{
    Pending,      // Awaiting AI processing
    Processing,   // Currently being processed
    Processed,    // AI analysis complete
    Reviewed,     // Admin viewed
    Error         // Processing failed
}
