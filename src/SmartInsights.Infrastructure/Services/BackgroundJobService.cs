using Hangfire;
using Microsoft.Extensions.Logging;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(ILogger<BackgroundJobService> logger)
    {
        _logger = logger;
    }

    public string EnqueueInputProcessing(Guid inputId)
    {
        _logger.LogInformation("Enqueuing AI processing for input {InputId}", inputId);

        var jobId = BackgroundJob.Enqueue<AIProcessingJobs>(
            job => job.ProcessInputAsync(inputId, CancellationToken.None));

        return jobId;
    }

    public string EnqueueInquirySummaryGeneration(Guid inquiryId)
    {
        _logger.LogInformation("Enqueuing inquiry summary generation for {InquiryId}", inquiryId);

        var jobId = BackgroundJob.Enqueue<AIProcessingJobs>(
            job => job.GenerateInquirySummaryAsync(inquiryId, CancellationToken.None));

        return jobId;
    }

    public string EnqueueTopicSummaryGeneration(Guid topicId)
    {
        _logger.LogInformation("Enqueuing topic summary generation for {TopicId}", topicId);

        var jobId = BackgroundJob.Enqueue<AIProcessingJobs>(
            job => job.GenerateTopicSummaryAsync(topicId, CancellationToken.None));

        return jobId;
    }

    public void ScheduleRecurringInputProcessing()
    {
        _logger.LogInformation("Scheduling recurring input processing job");

        // Run every 5 minutes to process any pending inputs
        RecurringJob.AddOrUpdate<AIProcessingJobs>(
            "process-pending-inputs",
            job => job.ProcessPendingInputsAsync(CancellationToken.None),
            "*/5 * * * *"); // Every 5 minutes
    }

    public void ScheduleRecurringInquirySummaries()
    {
        _logger.LogInformation("Scheduling recurring inquiry summary generation");

        // Run daily at 2 AM to regenerate summaries for active inquiries
        RecurringJob.AddOrUpdate<AIProcessingJobs>(
            "generate-inquiry-summaries",
            job => job.GenerateAllInquirySummariesAsync(CancellationToken.None),
            "0 2 * * *"); // Daily at 2 AM
    }

    public void ScheduleRecurringTopicSummaries()
    {
        _logger.LogInformation("Scheduling recurring topic summary generation");

        // Run daily at 3 AM to regenerate summaries for active topics
        RecurringJob.AddOrUpdate<AIProcessingJobs>(
            "generate-topic-summaries",
            job => job.GenerateAllTopicSummariesAsync(CancellationToken.None),
            "0 3 * * *"); // Daily at 3 AM
    }
}
