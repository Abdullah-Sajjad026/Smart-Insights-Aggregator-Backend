using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;
using SmartInsights.Infrastructure.Data;

namespace SmartInsights.Infrastructure.Services;

/// <summary>
/// Background jobs for AI processing
/// These methods are executed by Hangfire in the background
/// </summary>
public class AIProcessingJobs
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<AIProcessingJobs> _logger;

    public AIProcessingJobs(
        ApplicationDbContext context,
        IAIService aiService,
        ILogger<AIProcessingJobs> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Process a single input with AI analysis
    /// </summary>
    public async Task ProcessInputAsync(Guid inputId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting AI processing for input {InputId}", inputId);

            var input = await _context.Inputs
                .Include(i => i.User)
                .Include(i => i.User.Department)
                .Include(i => i.Inquiry)
                .FirstOrDefaultAsync(i => i.Id == inputId, cancellationToken);

            if (input == null)
            {
                _logger.LogWarning("Input {InputId} not found", inputId);
                return;
            }

            if (input.Status != InputStatus.Pending)
            {
                _logger.LogInformation("Input {InputId} already processed (Status: {Status})", inputId, input.Status);
                return;
            }

            // Step 1: Analyze input with AI
            _logger.LogInformation("Analyzing input {InputId} with AI", inputId);
            var analysis = await _aiService.AnalyzeInputAsync(input.Body, input.Type);

            // Step 2: Update input with analysis results
            input.Sentiment = analysis.Sentiment;
            input.Tone = analysis.Tone;
            input.UrgencyScore = analysis.Urgency;
            input.ImportanceScore = analysis.Importance;
            input.ClarityScore = analysis.Clarity;
            input.QualityScore = analysis.Quality;
            input.HelpfulnessScore = analysis.Helpfulness;
            input.SeverityLevel = analysis.Severity;
            input.AIProcessedAt = DateTime.UtcNow;

            // Step 3: Find or assign theme
            var theme = await FindOrCreateThemeAsync(analysis.ExtractedTheme, cancellationToken);
            if (theme != null)
            {
                input.ThemeId = theme.Id;
            }

            // Step 4: Generate or find topic (only for general feedback)
            if (input.Type == InputType.General)
            {
                _logger.LogInformation("Generating topic for input {InputId}", inputId);
                var departmentId = input.User?.DepartmentId;
                var topic = await _aiService.GenerateOrFindTopicAsync(input.Body, departmentId);
                input.TopicId = topic.Id;
            }

            // Step 5: Update status to Reviewed
            input.Status = InputStatus.Reviewed;
            input.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AI processing completed for input {InputId}: Sentiment={Sentiment}, Score={Score:F2}, Topic={TopicId}",
                inputId,
                analysis.Sentiment,
                analysis.Score,
                input.TopicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input {InputId} with AI", inputId);

            // Update input status to indicate error
            try
            {
                var input = await _context.Inputs.FindAsync(new object[] { inputId }, cancellationToken);
                if (input != null)
                {
                    // Keep as Pending so it can be retried
                    input.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update input {InputId} after processing error", inputId);
            }

            throw; // Re-throw for Hangfire to handle retry
        }
    }

    /// <summary>
    /// Process all pending inputs (scheduled job)
    /// </summary>
    public async Task ProcessPendingInputsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting batch processing of pending inputs");

            var pendingInputs = await _context.Inputs
                .Where(i => i.Status == InputStatus.Pending)
                .Where(i => i.AIProcessedAt == null)
                .OrderBy(i => i.CreatedAt)
                .Take(50) // Process max 50 at a time
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (!pendingInputs.Any())
            {
                _logger.LogInformation("No pending inputs to process");
                return;
            }

            _logger.LogInformation("Found {Count} pending inputs to process", pendingInputs.Count);

            foreach (var inputId in pendingInputs)
            {
                try
                {
                    await ProcessInputAsync(inputId, cancellationToken);

                    // Small delay to avoid rate limiting
                    await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process input {InputId} in batch", inputId);
                    // Continue with next input
                }
            }

            _logger.LogInformation("Batch processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch processing of pending inputs");
        }
    }

    /// <summary>
    /// Generate executive summary for an inquiry
    /// </summary>
    public async Task GenerateInquirySummaryAsync(Guid inquiryId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating summary for inquiry {InquiryId}", inquiryId);

            var inquiry = await _context.Inquiries
                .FirstOrDefaultAsync(i => i.Id == inquiryId, cancellationToken);

            if (inquiry == null)
            {
                _logger.LogWarning("Inquiry {InquiryId} not found", inquiryId);
                return;
            }

            // Get all responses for this inquiry
            var inputs = await _context.Inputs
                .Where(i => i.InquiryId == inquiryId)
                .Where(i => i.AIProcessedAt != null) // Only processed inputs
                .ToListAsync(cancellationToken);

            if (!inputs.Any())
            {
                _logger.LogInformation("No processed inputs found for inquiry {InquiryId}", inquiryId);
                return;
            }

            // Generate summary
            var summary = await _aiService.GenerateInquirySummaryAsync(inquiryId, inputs);

            // Save summary
            inquiry.ExecutiveSummary = System.Text.Json.JsonSerializer.Serialize(summary);
            inquiry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated summary for inquiry {InquiryId} with {InputCount} responses",
                inquiryId,
                inputs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for inquiry {InquiryId}", inquiryId);
            throw;
        }
    }

    /// <summary>
    /// Generate executive summary for a topic
    /// </summary>
    public async Task GenerateTopicSummaryAsync(Guid topicId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating summary for topic {TopicId}", topicId);

            var topic = await _context.Topics
                .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);

            if (topic == null)
            {
                _logger.LogWarning("Topic {TopicId} not found", topicId);
                return;
            }

            // Get all inputs for this topic
            var inputs = await _context.Inputs
                .Where(i => i.TopicId == topicId)
                .Where(i => i.AIProcessedAt != null)
                .ToListAsync(cancellationToken);

            if (!inputs.Any())
            {
                _logger.LogInformation("No processed inputs found for topic {TopicId}", topicId);
                return;
            }

            // Generate summary
            var summary = await _aiService.GenerateTopicSummaryAsync(topicId, inputs);

            // Save summary
            topic.ExecutiveSummary = System.Text.Json.JsonSerializer.Serialize(summary);
            topic.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated summary for topic {TopicId} ({TopicName}) with {InputCount} inputs",
                topicId,
                topic.Name,
                inputs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for topic {TopicId}", topicId);
            throw;
        }
    }

    /// <summary>
    /// Generate summaries for all active inquiries (scheduled job)
    /// </summary>
    public async Task GenerateAllInquirySummariesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting batch generation of inquiry summaries");

            var activeInquiries = await _context.Inquiries
                .Where(i => i.Status == InquiryStatus.Active)
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (!activeInquiries.Any())
            {
                _logger.LogInformation("No active inquiries found");
                return;
            }

            _logger.LogInformation("Found {Count} active inquiries", activeInquiries.Count);

            foreach (var inquiryId in activeInquiries)
            {
                try
                {
                    await GenerateInquirySummaryAsync(inquiryId, cancellationToken);

                    // Small delay
                    await Task.Delay(2000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate summary for inquiry {InquiryId}", inquiryId);
                    // Continue with next inquiry
                }
            }

            _logger.LogInformation("Batch summary generation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch summary generation");
        }
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private async Task<Theme?> FindOrCreateThemeAsync(string themeName, CancellationToken cancellationToken)
    {
        try
        {
            // Try to parse theme type from string
            if (!Enum.TryParse<ThemeType>(themeName, true, out var themeType))
            {
                themeType = ThemeType.Other;
            }

            // Find existing theme
            var existingTheme = await _context.Themes
                .FirstOrDefaultAsync(t => t.Type == themeType, cancellationToken);

            if (existingTheme != null)
            {
                return existingTheme;
            }

            // Create new theme
            var newTheme = new Theme
            {
                Id = Guid.NewGuid(),
                Name = themeName,
                Type = themeType,
                Description = $"Auto-generated theme for {themeName} related feedback",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Themes.Add(newTheme);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new theme: {ThemeName}", themeName);

            return newTheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding or creating theme: {ThemeName}", themeName);
            return null;
        }
    }
}
