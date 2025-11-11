using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;
using Mscc.GenerativeAI;

namespace SmartInsights.Infrastructure.Services;

/// <summary>
/// Google Gemini AI Service with retry logic, caching, and cost tracking
/// </summary>
public class GeminiAIService : IAIService
{
    private readonly GoogleAI _client;
    private readonly string _model;
    private readonly ILogger<GeminiAIService> _logger;
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

    public GeminiAIService(
        IConfiguration configuration,
        ILogger<GeminiAIService> logger,
        IRepository<Topic> topicRepository,
        IRepository<Theme> themeRepository,
        IMemoryCache cache,
        IAICostTrackingService costTracking)
    {
        var apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");
        _model = configuration["Gemini:Model"] ?? "gemini-pro";

        _client = new GoogleAI(apiKey);
        _logger = logger;
        _topicRepository = topicRepository;
        _themeRepository = themeRepository;
        _cache = cache;
        _costTracking = costTracking;

        // Load configuration with defaults
        _temperature = float.Parse(configuration["Gemini:Temperature"] ?? "0.7");
        _maxTokens = int.Parse(configuration["Gemini:MaxTokens"] ?? "2000");
        _maxRetries = int.Parse(configuration["Gemini:MaxRetries"] ?? "3");
        _cacheExpiration = TimeSpan.FromHours(
            int.Parse(configuration["Gemini:CacheExpirationHours"] ?? "24"));

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<Exception>(ex => ex.Message.Contains("rate limit") || ex.Message.Contains("quota"))
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Gemini AI request failed. Retry {RetryCount} after {DelaySeconds}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    public async Task<InputAnalysisResult> AnalyzeInputAsync(string body, InputType type)
    {
        var cacheKey = $"gemini_analysis_{HashString(body)}_{type}";

        // Check cache first
        if (_cache.TryGetValue<InputAnalysisResult>(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Using cached Gemini analysis for input");
            return cachedResult!;
        }

        try
        {
            var prompt = type == InputType.General
                ? BuildEnhancedGeneralInputAnalysisPrompt(body)
                : BuildEnhancedInquiryInputAnalysisPrompt(body);

            var response = await CallGeminiWithRetryAsync(prompt, "input_analysis");
            var result = ParseAnalysisResponse(response);

            // Cache the result
            _cache.Set(cacheKey, result, _cacheExpiration);

            _logger.LogInformation(
                "Input analyzed with Gemini: Sentiment={Sentiment}, Severity={Severity}, Score={Score}",
                result.Sentiment, result.Severity, result.Score);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze input with Gemini");
            throw;
        }
    }

    public async Task<Topic> GenerateOrFindTopicAsync(string body, Guid? departmentId)
    {
        var cacheKey = $"gemini_topic_{HashString(body)}_{departmentId}";

        if (_cache.TryGetValue<Topic>(cacheKey, out var cachedTopic))
        {
            _logger.LogInformation("Using cached topic from Gemini");
            return cachedTopic!;
        }

        try
        {
            var prompt = BuildTopicGenerationPrompt(body);
            var response = await CallGeminiWithRetryAsync(prompt, "topic_generation");

            var topicName = response.Trim().Trim('"');

            // Try to find existing topic
            var existingTopic = await _topicRepository.FindAsync(t =>
                t.Name.ToLower() == topicName.ToLower() &&
                (departmentId == null || t.DepartmentId == departmentId));

            if (existingTopic != null)
            {
                _cache.Set(cacheKey, existingTopic, _cacheExpiration);
                return existingTopic;
            }

            // Create new topic
            var newTopic = new Topic
            {
                Id = Guid.NewGuid(),
                Name = topicName,
                DepartmentId = departmentId,
                CreatedAt = DateTime.UtcNow
            };

            await _topicRepository.AddAsync(newTopic);
            _cache.Set(cacheKey, newTopic, _cacheExpiration);

            _logger.LogInformation("Created new topic with Gemini: {TopicName}", topicName);
            return newTopic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate/find topic with Gemini");
            throw;
        }
    }

    public async Task<ExecutiveSummary> GenerateInquirySummaryAsync(Guid inquiryId, List<Input> inputs)
    {
        // Check for empty inputs first
        if (!inputs.Any())
        {
            return CreateEmptySummary();
        }

        var cacheKey = $"gemini_inquiry_summary_{inquiryId}_{inputs.Count}_{inputs.Max(i => i.UpdatedAt):yyyyMMddHHmmss}";

        if (_cache.TryGetValue<ExecutiveSummary>(cacheKey, out var cachedSummary))
        {
            _logger.LogInformation("Using cached inquiry summary from Gemini");
            return cachedSummary!;
        }

        try
        {
            var prompt = BuildInquirySummaryPrompt(inputs);
            var response = await CallGeminiWithRetryAsync(prompt, "inquiry_summary");

            var summary = ParseExecutiveSummaryResponse(response);
            _cache.Set(cacheKey, summary, _cacheExpiration);

            _logger.LogInformation("Generated inquiry summary with Gemini for {InputCount} responses", inputs.Count);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate inquiry summary with Gemini");
            throw;
        }
    }

    public async Task<ExecutiveSummary> GenerateTopicSummaryAsync(Guid topicId, List<Input> inputs)
    {
        // Check for empty inputs first
        if (!inputs.Any())
        {
            return CreateEmptySummary();
        }

        var cacheKey = $"gemini_topic_summary_{topicId}_{inputs.Count}_{inputs.Max(i => i.UpdatedAt):yyyyMMddHHmmss}";

        if (_cache.TryGetValue<ExecutiveSummary>(cacheKey, out var cachedSummary))
        {
            _logger.LogInformation("Using cached topic summary from Gemini");
            return cachedSummary!;
        }

        try
        {
            var prompt = BuildTopicSummaryPrompt(inputs);
            var response = await CallGeminiWithRetryAsync(prompt, "topic_summary");

            var summary = ParseExecutiveSummaryResponse(response);
            _cache.Set(cacheKey, summary, _cacheExpiration);

            _logger.LogInformation("Generated topic summary with Gemini for {InputCount} inputs", inputs.Count);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate topic summary with Gemini");
            throw;
        }
    }

    private async Task<string> CallGeminiWithRetryAsync(string prompt, string operationType)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var model = _client.GenerativeModel(_model);

            var generateContentRequest = new GenerateContentRequest
            {
                Contents = new List<Content>
                {
                    new Content
                    {
                        Parts = new List<IPart>
                        {
                            new TextData { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = _temperature,
                    MaxOutputTokens = _maxTokens
                }
            };

            var response = await model.GenerateContent(generateContentRequest);

            if (response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault() is TextData textPart)
            {
                var text = textPart.Text;

                // Track cost (estimate for Gemini - adjust as needed)
                var promptTokens = EstimateTokens(prompt);
                var completionTokens = EstimateTokens(text);

                await _costTracking.TrackUsageAsync(
                    operationType,
                    promptTokens,
                    completionTokens,
                    CalculateGeminiCost(promptTokens, completionTokens));

                return text;
            }

            throw new InvalidOperationException("No valid response from Gemini API");
        });
    }

    private string BuildEnhancedGeneralInputAnalysisPrompt(string body)
    {
        return $@"Analyze this student feedback and return ONLY a valid JSON object with these exact fields:

Feedback: ""{body}""

Return JSON format:
{{
  ""sentiment"": ""Positive"" or ""Negative"" or ""Neutral"",
  ""tone"": ""Formal"" or ""Informal"" or ""Frustrated"" or ""Appreciative"" or ""Concerned"" or ""Neutral"",
  ""urgency"": 0.0 to 1.0,
  ""importance"": 0.0 to 1.0,
  ""clarity"": 0.0 to 1.0,
  ""quality"": 0.0 to 1.0,
  ""helpfulness"": 0.0 to 1.0,
  ""theme"": ""Infrastructure"" or ""Academic"" or ""Technology"" or ""Facilities"" or ""Administrative"" or ""Social"" or ""Other""
}}

Guidelines:
- Urgency: How quickly this needs attention (0=low, 1=urgent)
- Importance: Impact on student experience (0=minor, 1=critical)
- Clarity: How clear and specific is the feedback (0=vague, 1=very clear)
- Quality: Overall quality of feedback (0=poor, 1=excellent)
- Helpfulness: How actionable/useful is this feedback (0=not useful, 1=very useful)

Return ONLY the JSON, no additional text.";
    }

    private string BuildEnhancedInquiryInputAnalysisPrompt(string body)
    {
        return $@"Analyze this student response to an inquiry and return ONLY a valid JSON object:

Response: ""{body}""

Return JSON format:
{{
  ""sentiment"": ""Positive"" or ""Negative"" or ""Neutral"",
  ""tone"": ""Formal"" or ""Informal"" or ""Frustrated"" or ""Appreciative"" or ""Concerned"" or ""Neutral"",
  ""urgency"": 0.0 to 1.0,
  ""importance"": 0.0 to 1.0,
  ""clarity"": 0.0 to 1.0,
  ""quality"": 0.0 to 1.0,
  ""helpfulness"": 0.0 to 1.0,
  ""theme"": ""Infrastructure"" or ""Academic"" or ""Technology"" or ""Facilities"" or ""Administrative"" or ""Social"" or ""Other""
}}

Return ONLY the JSON, no additional text.";
    }

    private string BuildTopicGenerationPrompt(string body)
    {
        return $@"Based on this student feedback, generate a concise topic name (3-6 words) that captures the main subject.

Feedback: ""{body}""

Return ONLY the topic name, nothing else. Examples:
- ""Computer Lab Equipment Upgrade""
- ""Library Study Spaces""
- ""Course Registration System""
- ""Wi-Fi Connectivity Issues""

Topic name:";
    }

    private string BuildInquirySummaryPrompt(List<Input> inputs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze these student responses to an inquiry and create an executive summary.");
        sb.AppendLine($"\nTotal Responses: {inputs.Count}\n");
        sb.AppendLine("Responses:");

        foreach (var input in inputs.Take(50))
        {
            sb.AppendLine($"- {input.Body}");
        }

        sb.AppendLine(@"

Create an executive summary in JSON format:
{
  ""topics"": [""topic1"", ""topic2"", ""topic3""],
  ""executiveSummary"": ""A concise 2-3 paragraph summary of key findings"",
  ""suggestedActions"": [
    {
      ""action"": ""Action description"",
      ""impact"": ""Expected impact"",
      ""challenges"": ""Potential challenges"",
      ""responseCount"": number,
      ""supportingReasoning"": ""Why this action""
    }
  ]
}

Return ONLY the JSON, no additional text.");

        return sb.ToString();
    }

    private string BuildTopicSummaryPrompt(List<Input> inputs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze these student feedback items on the same topic and create an executive summary.");
        sb.AppendLine($"\nTotal Inputs: {inputs.Count}\n");
        sb.AppendLine("Feedback:");

        foreach (var input in inputs.Take(50))
        {
            sb.AppendLine($"- {input.Body}");
        }

        sb.AppendLine(@"

Create an executive summary in JSON format (same format as inquiry summary).

Return ONLY the JSON, no additional text.");

        return sb.ToString();
    }

    private InputAnalysisResult ParseAnalysisResponse(string response)
    {
        try
        {
            // Clean up response - remove markdown code blocks if present
            var jsonText = response.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var sentiment = Enum.Parse<Sentiment>(root.GetProperty("sentiment").GetString() ?? "Neutral", true);
            var tone = Enum.Parse<Tone>(root.GetProperty("tone").GetString() ?? "Neutral", true);
            var urgency = root.GetProperty("urgency").GetDouble();
            var importance = root.GetProperty("importance").GetDouble();
            var clarity = root.GetProperty("clarity").GetDouble();
            var quality = root.GetProperty("quality").GetDouble();
            var helpfulness = root.GetProperty("helpfulness").GetDouble();
            var theme = root.GetProperty("theme").GetString() ?? "Other";

            var score = (urgency + importance + clarity + quality + helpfulness) / 5.0;
            var severity = score >= 0.7 ? 3 : score >= 0.4 ? 2 : 1;

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
            _logger.LogError(ex, "Failed to parse Gemini analysis response: {Response}", response);
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    private ExecutiveSummary ParseExecutiveSummaryResponse(string response)
    {
        try
        {
            // Clean up response
            var jsonText = response.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var summary = JsonSerializer.Deserialize<ExecutiveSummary>(jsonText, options);

            if (summary == null)
            {
                throw new InvalidOperationException("Deserialized summary is null");
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini executive summary response: {Response}", response);
            throw new InvalidOperationException("Failed to parse AI summary response", ex);
        }
    }

    private ExecutiveSummary CreateEmptySummary()
    {
        return new ExecutiveSummary
        {
            Topics = new List<string> { "No responses yet" },
            ExecutiveSummaryData = "No responses have been submitted yet.",
            SuggestedPrioritizedActions = new List<SuggestedAction>()
        };
    }

    private string HashString(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }

    private int EstimateTokens(string text)
    {
        // Rough estimation: ~4 characters per token
        return text.Length / 4;
    }

    private decimal CalculateGeminiCost(int promptTokens, int completionTokens)
    {
        // Gemini Pro pricing (as of 2024, adjust as needed):
        // Input: $0.00025 / 1K tokens
        // Output: $0.0005 / 1K tokens
        decimal inputCost = (promptTokens / 1000m) * 0.00025m;
        decimal outputCost = (completionTokens / 1000m) * 0.0005m;
        return inputCost + outputCost;
    }
}
