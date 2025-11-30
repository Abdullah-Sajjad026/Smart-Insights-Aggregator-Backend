namespace SmartInsights.Application.Interfaces;

/// <summary>
/// Service for managing background jobs for AI processing
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue AI processing for a specific input
    /// </summary>
    string EnqueueInputProcessing(Guid inputId);

    /// <summary>
    /// Enqueue executive summary generation for an inquiry
    /// </summary>
    string EnqueueInquirySummaryGeneration(Guid inquiryId);

    /// <summary>
    /// Enqueue executive summary generation for a topic
    /// </summary>
    string EnqueueTopicSummaryGeneration(Guid topicId, bool bypassCache = false);

    /// <summary>
    /// Schedule recurring job to process pending inputs
    /// </summary>
    void ScheduleRecurringInputProcessing();

    /// <summary>
    /// Schedule recurring job to generate inquiry summaries
    /// </summary>
    void ScheduleRecurringInquirySummaries();

    /// <summary>
    /// Schedule recurring job to generate topic summaries
    /// </summary>
    void ScheduleRecurringTopicSummaries();
}
