using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Infrastructure.Services;

/// <summary>
/// Improved Azure OpenAI Service with retry logic, caching, better prompts, and cost tracking
/// </summary>
public class ImprovedAzureOpenAIService : IAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<ImprovedAzureOpenAIService> _logger;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Theme> _themeRepository;
    private readonly IMemoryCache _cache;
    private readonly IAICostTrackingService _costTracking;
    private readonly AsyncRetryPolicy _retryPolicy;

    // Configuration
    private readonly float _temperature;
    private readonly int _maxTokens;
    private readonly int _maxRetries;
    private readonly TimeSpan _cacheExpiration;

    public ImprovedAzureOpenAIService(
        IConfiguration configuration,
        ILogger<ImprovedAzureOpenAIService> logger,
        IRepository<Topic> topicRepository,
        IRepository<Theme> themeRepository,
        IMemoryCache cache,
        IAICostTrackingService costTracking)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        _deploymentName = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("Azure OpenAI deployment name not configured");

        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _logger = logger;
        _topicRepository = topicRepository;
        _themeRepository = themeRepository;
        _cache = cache;
        _costTracking = costTracking;

        // Load configuration with defaults
        _temperature = float.Parse(configuration["AzureOpenAI:Temperature"] ?? "0.7");
        _maxTokens = int.Parse(configuration["AzureOpenAI:MaxTokens"] ?? "2000");
        _maxRetries = int.Parse(configuration["AzureOpenAI:MaxRetries"] ?? "3");
        _cacheExpiration = TimeSpan.FromHours(
            int.Parse(configuration["AzureOpenAI:CacheExpirationHours"] ?? "24"));

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<RequestFailedException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "AI request failed. Retry {RetryCount} after {DelaySeconds}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    public async Task<InputAnalysisResult> AnalyzeInputAsync(string body, InputType type)
    {
        var cacheKey = $"analysis_{HashString(body)}_{type}";

        // Check cache first
        if (_cache.TryGetValue<InputAnalysisResult>(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Using cached analysis for input");
            return cachedResult!;
        }

        try
        {
            var prompt = type == InputType.General
                ? BuildEnhancedGeneralInputAnalysisPrompt(body)
                : BuildEnhancedInquiryInputAnalysisPrompt(body);

            var (response, usage) = await CallOpenAIWithRetryAsync(prompt, "input_analysis");
            var result = ParseAndValidateAnalysisResponse(response);

            // Cache the result
            _cache.Set(cacheKey, result, _cacheExpiration);

            // Track cost
            await TrackCostAsync("input_analysis", usage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing input after all retries");

            // Return safe defaults
            return new InputAnalysisResult
            {
                Sentiment = Sentiment.Neutral,
                Tone = Tone.Neutral,
                Urgency = 0.5,
                Importance = 0.5,
                Clarity = 0.5,
                Quality = 0.5,
                Helpfulness = 0.5,
                Score = 0.5,
                Severity = 2,
                ExtractedTheme = "General"
            };
        }
    }

    public async Task<Topic> GenerateOrFindTopicAsync(string body, Guid? departmentId)
    {
        try
        {
            var prompt = BuildEnhancedTopicGenerationPrompt(body);
            var (response, usage) = await CallOpenAIWithRetryAsync(prompt, "topic_generation");
            var topicName = CleanTopicName(response);

            // Track cost
            await TrackCostAsync("topic_generation", usage);

            // Check for similar existing topics using enhanced similarity
            var existingTopics = await _topicRepository.FindAsync(t =>
                t.DepartmentId == departmentId || t.DepartmentId == null);

            var matchingTopic = FindBestMatchingTopic(existingTopics, topicName);

            if (matchingTopic != null)
            {
                _logger.LogInformation(
                    "Found existing topic '{ExistingTopic}' for '{NewTopic}'",
                    matchingTopic.Name,
                    topicName);
                return matchingTopic;
            }

            // Create new topic
            var newTopic = new Topic
            {
                Id = Guid.NewGuid(),
                Name = topicName,
                DepartmentId = departmentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _topicRepository.AddAsync(newTopic);
            _logger.LogInformation("Created new topic: {TopicName}", topicName);

            return newTopic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topic");

            return new Topic
            {
                Id = Guid.NewGuid(),
                Name = "General Feedback",
                DepartmentId = departmentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<ExecutiveSummary> GenerateInquirySummaryAsync(Guid inquiryId, List<Input> inputs)
    {
        // Check for empty inputs first
        if (!inputs.Any())
        {
            return CreateEmptySummary();
        }

        var cacheKey = $"inquiry_summary_{inquiryId}_{inputs.Count}_{inputs.Max(i => i.UpdatedAt):yyyyMMddHHmmss}";

        // Check cache
        if (_cache.TryGetValue<ExecutiveSummary>(cacheKey, out var cachedSummary))
        {
            _logger.LogInformation("Using cached inquiry summary for {InquiryId}", inquiryId);
            return cachedSummary!;
        }

        try
        {

            var prompt = BuildEnhancedInquirySummaryPrompt(inputs);
            var (response, usage) = await CallOpenAIWithRetryAsync(prompt, "inquiry_summary", maxTokens: 3000);
            var summary = ParseAndValidateExecutiveSummary(response);

            // Cache the summary
            _cache.Set(cacheKey, summary, _cacheExpiration);

            // Track cost
            await TrackCostAsync("inquiry_summary", usage);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inquiry summary for {InquiryId}", inquiryId);
            return CreateEmptySummary();
        }
    }

    public async Task<ExecutiveSummary> GenerateTopicSummaryAsync(Guid topicId, List<Input> inputs)
    {
        // Check for empty inputs first
        if (!inputs.Any())
        {
            return CreateEmptySummary();
        }

        var cacheKey = $"topic_summary_{topicId}_{inputs.Count}_{inputs.Max(i => i.UpdatedAt):yyyyMMddHHmmss}";

        if (_cache.TryGetValue<ExecutiveSummary>(cacheKey, out var cachedSummary))
        {
            _logger.LogInformation("Using cached topic summary for {TopicId}", topicId);
            return cachedSummary!;
        }

        try
        {

            var prompt = BuildEnhancedTopicSummaryPrompt(inputs);
            var (response, usage) = await CallOpenAIWithRetryAsync(prompt, "topic_summary", maxTokens: 3000);
            var summary = ParseAndValidateExecutiveSummary(response);

            _cache.Set(cacheKey, summary, _cacheExpiration);
            await TrackCostAsync("topic_summary", usage);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topic summary for {TopicId}", topicId);
            return CreateEmptySummary();
        }
    }

    // ============================================================================
    // Private Helper Methods - API Calls with Retry
    // ============================================================================

    private async Task<(string Response, CompletionsUsage Usage)> CallOpenAIWithRetryAsync(
        string prompt,
        string operation,
        int? maxTokens = null)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt()),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = maxTokens ?? _maxTokens,
                Temperature = _temperature
            };

            _logger.LogDebug("Calling Azure OpenAI for operation: {Operation}", operation);

            var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;
            var usage = response.Value.Usage;

            _logger.LogInformation(
                "AI request completed. Tokens - Prompt: {Prompt}, Completion: {Completion}, Total: {Total}",
                usage.PromptTokens,
                usage.CompletionTokens,
                usage.TotalTokens);

            return (content, usage);
        });
    }

    // ============================================================================
    // Enhanced Prompts with Few-Shot Examples
    // ============================================================================

    private string GetSystemPrompt()
    {
        return @"You are an expert AI assistant specialized in analyzing student feedback for KFUEIT University in Pakistan.

Your responsibilities:
- Analyze sentiment, tone, and quality metrics of student feedback
- Generate concise, actionable topic names
- Create executive summaries with strategic insights
- Provide structured JSON responses

Context: KFUEIT is a leading engineering university with departments including Computer Science, Software Engineering, Electrical Engineering, Mechanical Engineering, and Civil Engineering.

Always provide responses in valid JSON format without markdown code blocks or additional text.";
    }

    private string BuildEnhancedGeneralInputAnalysisPrompt(string body)
    {
        return $@"Analyze this student feedback from KFUEIT University and provide a JSON response:

FEEDBACK:
""{body}""

Provide a JSON response with this exact structure:
{{
    ""sentiment"": ""Positive|Neutral|Negative"",
    ""tone"": ""Positive|Neutral|Negative"",
    ""urgency"": 0.0 to 1.0,
    ""importance"": 0.0 to 1.0,
    ""clarity"": 0.0 to 1.0,
    ""quality"": 0.0 to 1.0,
    ""helpfulness"": 0.0 to 1.0,
    ""theme"": ""Infrastructure|Academic|Technology|Facilities|Administrative|Social|Other""
}}

RATING GUIDELINES:

Sentiment & Tone:
- Positive: Praise, appreciation, satisfaction
- Neutral: Factual observations, suggestions without emotion
- Negative: Complaints, criticism, dissatisfaction

Urgency (0.0-1.0):
- 0.9-1.0: Immediate safety/security concerns, system outages
- 0.7-0.8: Significant disruptions affecting many students
- 0.5-0.6: Important but not time-critical issues
- 0.0-0.4: General suggestions, minor inconveniences

Importance (0.0-1.0):
- 0.9-1.0: Affects entire university or critical infrastructure
- 0.7-0.8: Affects multiple departments or large student groups
- 0.5-0.6: Affects specific department or smaller groups
- 0.0-0.4: Individual concerns or minor issues

Clarity (0.0-1.0):
- 0.9-1.0: Crystal clear, specific details, actionable
- 0.7-0.8: Clear main point, some details provided
- 0.5-0.6: Understandable but vague
- 0.0-0.4: Unclear, rambling, or confusing

Quality (0.0-1.0):
- 0.9-1.0: Constructive, specific, with solutions
- 0.7-0.8: Constructive with details
- 0.5-0.6: Valid but lacks detail
- 0.0-0.4: Vague complaints without substance

Helpfulness (0.0-1.0):
- 0.9-1.0: Highly actionable, enables immediate decisions
- 0.7-0.8: Useful for planning and improvements
- 0.5-0.6: Some value but needs clarification
- 0.0-0.4: Not actionable

EXAMPLES:

Example 1:
Feedback: ""The WiFi in the library constantly disconnects. I can't complete my assignments. This has been happening for 2 weeks now.""
Response: {{""sentiment"":""Negative"",""tone"":""Negative"",""urgency"":0.75,""importance"":0.8,""clarity"":0.9,""quality"":0.8,""helpfulness"":0.85,""theme"":""Technology""}}

Example 2:
Feedback: ""Great job on the new cafeteria menu! More variety and healthier options. Keep it up!""
Response: {{""sentiment"":""Positive"",""tone"":""Positive"",""urgency"":0.1,""importance"":0.4,""clarity"":0.8,""quality"":0.7,""helpfulness"":0.6,""theme"":""Facilities""}}

Example 3:
Feedback: ""Lab equipment in EE department needs upgrade. Current oscilloscopes are outdated.""
Response: {{""sentiment"":""Neutral"",""tone"":""Neutral"",""urgency"":0.5,""importance"":0.7,""clarity"":0.8,""quality"":0.75,""helpfulness"":0.8,""theme"":""Academic""}}

Provide ONLY the JSON response for the given feedback, nothing else.";
    }

    private string BuildEnhancedInquiryInputAnalysisPrompt(string body)
    {
        return $@"Analyze this inquiry response from a KFUEIT University student:

RESPONSE:
""{body}""

Provide a JSON response with this exact structure:
{{
    ""sentiment"": ""Positive|Neutral|Negative"",
    ""tone"": ""Positive|Neutral|Negative"",
    ""urgency"": 0.0 to 1.0,
    ""importance"": 0.0 to 1.0,
    ""clarity"": 0.0 to 1.0,
    ""quality"": 0.0 to 1.0,
    ""helpfulness"": 0.0 to 1.0
}}

Use the same rating guidelines as general feedback analysis. Inquiry responses are typically more structured since they answer specific questions.

Provide ONLY the JSON response, nothing else.";
    }

    private string BuildEnhancedTopicGenerationPrompt(string body)
    {
        return $@"Generate a concise topic name (3-6 words max) that categorizes this student feedback:

FEEDBACK:
""{body}""

GUIDELINES:
- Be specific but concise
- Use title case
- Focus on the main issue or subject
- Avoid generic terms like ""Issue"" or ""Problem"" unless necessary
- Make it searchable and groupable

EXAMPLES:

Feedback: ""The WiFi in the library keeps disconnecting every few minutes.""
Topic: ""Library WiFi Connectivity""

Feedback: ""Final exam schedule has too many exams on the same day.""
Topic: ""Exam Scheduling Conflicts""

Feedback: ""Cafeteria food quality has decreased and prices increased.""
Topic: ""Cafeteria Quality and Pricing""

Feedback: ""Not enough parking spaces for students, especially during peak hours.""
Topic: ""Student Parking Shortage""

Feedback: ""Lab equipment in computer science department is outdated.""
Topic: ""CS Lab Equipment Upgrade""

Provide ONLY the topic name (3-6 words), nothing else. No quotes, no additional text.";
    }

    private string BuildEnhancedInquirySummaryPrompt(List<Input> inputs)
    {
        var feedbackList = new StringBuilder();
        var sampleSize = Math.Min(inputs.Count, 100); // Limit for token management

        foreach (var input in inputs.Take(sampleSize))
        {
            feedbackList.AppendLine($"- {input.Body}");
        }

        var positiveCount = inputs.Count(i => i.Sentiment == Sentiment.Positive);
        var neutralCount = inputs.Count(i => i.Sentiment == Sentiment.Neutral);
        var negativeCount = inputs.Count(i => i.Sentiment == Sentiment.Negative);

        return $@"You are analyzing {inputs.Count} student responses to an inquiry at KFUEIT University.

SENTIMENT DISTRIBUTION:
- Positive: {positiveCount} ({(positiveCount * 100.0 / inputs.Count):F1}%)
- Neutral: {neutralCount} ({(neutralCount * 100.0 / inputs.Count):F1}%)
- Negative: {negativeCount} ({(negativeCount * 100.0 / inputs.Count):F1}%)

RESPONSES (showing first {sampleSize}):
{feedbackList}

Generate an executive summary in this exact JSON format:
{{
    ""topics"": [""topic1"", ""topic2"", ""topic3""],
    ""executiveSummaryData"": {{
        ""headlineInsight"": ""One compelling sentence summarizing the key finding"",
        ""responseMix"": ""Brief overview of sentiment distribution and response quality"",
        ""keyTakeaways"": ""2-3 paragraphs with detailed analysis of patterns, common themes, and notable insights"",
        ""risks"": ""What problems could arise if issues are not addressed"",
        ""opportunities"": ""What improvements or positive outcomes are possible""
    }},
    ""suggestedPrioritizedActions"": [
        {{
            ""action"": ""Specific, actionable step"",
            ""impact"": ""HIGH|MEDIUM|LOW"",
            ""challenges"": ""Implementation obstacles to consider"",
            ""responseCount"": number_of_responses_supporting_this,
            ""supportingReasoning"": ""Why this action matters and evidence from responses""
        }}
    ]
}}

GUIDELINES:
- Identify 2-4 main topics/themes
- Headline should be data-driven and specific
- Key takeaways should be 150-250 words with concrete examples
- Suggest 3-5 prioritized actions
- Actions should be specific, measurable, and feasible
- Prioritize by impact and number of supporting responses
- Consider KFUEIT's context as an engineering university

Provide ONLY valid JSON, no markdown formatting, no additional text.";
    }

    private string BuildEnhancedTopicSummaryPrompt(List<Input> inputs)
    {
        return BuildEnhancedInquirySummaryPrompt(inputs); // Same format for now
    }

    // ============================================================================
    // Parsing and Validation
    // ============================================================================

    private InputAnalysisResult ParseAndValidateAnalysisResponse(string jsonResponse)
    {
        try
        {
            // Extract JSON from markdown if present
            var cleanJson = ExtractJson(jsonResponse);

            var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            // Parse with validation
            var sentiment = ParseEnum<Sentiment>(root, "sentiment", Sentiment.Neutral);
            var tone = ParseEnum<Tone>(root, "tone", Tone.Neutral);

            var urgency = ValidateScore(ParseDouble(root, "urgency", 0.5), "urgency");
            var importance = ValidateScore(ParseDouble(root, "importance", 0.5), "importance");
            var clarity = ValidateScore(ParseDouble(root, "clarity", 0.5), "clarity");
            var quality = ValidateScore(ParseDouble(root, "quality", 0.5), "quality");
            var helpfulness = ValidateScore(ParseDouble(root, "helpfulness", 0.5), "helpfulness");

            var score = (urgency + importance + clarity + quality + helpfulness) / 5.0;
            var severity = CalculateSeverity(score);

            var theme = root.TryGetProperty("theme", out var themeElement)
                ? themeElement.GetString() ?? "General"
                : "General";

            _logger.LogInformation(
                "Parsed analysis: Sentiment={Sentiment}, Score={Score:F2}, Severity={Severity}",
                sentiment,
                score,
                severity);

            return new InputAnalysisResult
            {
                Sentiment = sentiment,
                Tone = tone,
                Urgency = urgency,
                Importance = importance,
                Clarity = clarity,
                Quality = quality,
                Helpfulness = helpfulness,
                Score = score,
                Severity = severity,
                ExtractedTheme = theme
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI analysis response: {Response}", jsonResponse);
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    private ExecutiveSummary ParseAndValidateExecutiveSummary(string jsonResponse)
    {
        try
        {
            var cleanJson = ExtractJson(jsonResponse);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var summary = JsonSerializer.Deserialize<ExecutiveSummary>(cleanJson, options);

            if (summary == null)
            {
                throw new InvalidOperationException("Deserialized summary is null");
            }

            // Validate required fields
            if (summary.ExecutiveSummaryData == null || !summary.ExecutiveSummaryData.Any())
            {
                _logger.LogWarning("Executive summary missing data, using defaults");
                return CreateEmptySummary();
            }

            _logger.LogInformation(
                "Parsed executive summary with {TopicCount} topics and {ActionCount} actions",
                summary.Topics?.Count ?? 0,
                summary.SuggestedPrioritizedActions?.Count ?? 0);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing executive summary: {Response}", jsonResponse);
            return CreateEmptySummary();
        }
    }

    // ============================================================================
    // Topic Similarity with Levenshtein Distance
    // ============================================================================

    private Topic? FindBestMatchingTopic(IEnumerable<Topic> existingTopics, string newTopicName)
    {
        const double similarityThreshold = 0.7;

        Topic? bestMatch = null;
        double bestSimilarity = 0;

        foreach (var topic in existingTopics)
        {
            // Exact match
            if (string.Equals(topic.Name, newTopicName, StringComparison.OrdinalIgnoreCase))
            {
                return topic;
            }

            // Calculate similarity
            var similarity = CalculateStringSimilarity(topic.Name, newTopicName);

            if (similarity > bestSimilarity && similarity >= similarityThreshold)
            {
                bestSimilarity = similarity;
                bestMatch = topic;
            }
        }

        return bestMatch;
    }

    private double CalculateStringSimilarity(string s1, string s2)
    {
        // Normalize strings
        s1 = s1.ToLowerInvariant().Trim();
        s2 = s2.ToLowerInvariant().Trim();

        // Calculate Levenshtein distance
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        if (maxLength == 0) return 1.0;

        var similarity = 1.0 - ((double)distance / maxLength);

        // Bonus for word overlap
        var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commonWords = words1.Intersect(words2).Count();
        var totalWords = Math.Max(words1.Length, words2.Length);
        var wordOverlap = totalWords > 0 ? (double)commonWords / totalWords : 0;

        // Weighted average: 70% Levenshtein, 30% word overlap
        return (similarity * 0.7) + (wordOverlap * 0.3);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    // ============================================================================
    // Utility Methods
    // ============================================================================

    private async Task TrackCostAsync(string operation, CompletionsUsage usage)
    {
        // GPT-4 pricing (as of 2024): $0.03 per 1K prompt tokens, $0.06 per 1K completion tokens
        const double promptCostPer1K = 0.03;
        const double completionCostPer1K = 0.06;

        var cost = (usage.PromptTokens / 1000.0 * promptCostPer1K) +
                   (usage.CompletionTokens / 1000.0 * completionCostPer1K);

        await _costTracking.LogRequestAsync(
            operation,
            usage.PromptTokens,
            usage.CompletionTokens,
            cost);
    }

    private string ExtractJson(string response)
    {
        // Remove markdown code blocks if present
        response = response.Trim();

        if (response.StartsWith("```json"))
        {
            response = response["```json".Length..];
        }
        else if (response.StartsWith("```"))
        {
            response = response["```".Length..];
        }

        if (response.EndsWith("```"))
        {
            response = response[..^3];
        }

        // Find JSON boundaries
        var startIndex = response.IndexOf('{');
        var endIndex = response.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            response = response.Substring(startIndex, endIndex - startIndex + 1);
        }

        return response.Trim();
    }

    private T ParseEnum<T>(JsonElement root, string propertyName, T defaultValue) where T : struct
    {
        if (root.TryGetProperty(propertyName, out var element))
        {
            var value = element.GetString();
            if (!string.IsNullOrEmpty(value) && Enum.TryParse<T>(value, true, out var result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    private double ParseDouble(JsonElement root, string propertyName, double defaultValue)
    {
        if (root.TryGetProperty(propertyName, out var element))
        {
            if (element.TryGetDouble(out var value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    private double ValidateScore(double score, string scoreName)
    {
        if (score < 0 || score > 1)
        {
            _logger.LogWarning("{ScoreName} score {Score} out of range, clamping to [0,1]", scoreName, score);
            return Math.Clamp(score, 0, 1);
        }
        return score;
    }

    private int CalculateSeverity(double score)
    {
        return score switch
        {
            >= 0.75 => 3, // High
            >= 0.5 => 2,  // Medium
            _ => 1        // Low
        };
    }

    private string CleanTopicName(string topicName)
    {
        // Remove quotes and extra whitespace
        topicName = topicName.Trim().Trim('"', '\'');

        // Limit length
        if (topicName.Length > 100)
        {
            topicName = topicName.Substring(0, 97) + "...";
        }

        return topicName;
    }

    private string HashString(string input)
    {
        // Simple hash for cache keys
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(input)))
            .Substring(0, 16);
    }

    private ExecutiveSummary CreateEmptySummary()
    {
        return new ExecutiveSummary
        {
            Topics = new List<string>(),
            ExecutiveSummaryData = new Dictionary<string, string>
            {
                ["headlineInsight"] = "Insufficient data for analysis",
                ["responseMix"] = "No responses available",
                ["keyTakeaways"] = "Not enough feedback to generate insights",
                ["risks"] = "N/A",
                ["opportunities"] = "N/A"
            },
            SuggestedPrioritizedActions = new List<SuggestedAction>()
        };
    }
}
