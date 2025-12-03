using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// Google Gemini AI Service using REST API with retry logic, caching, and cost tracking
/// </summary>
public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
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
        IAICostTrackingService costTracking,
        IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured. Set Gemini:ApiKey in configuration.");
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash-latest";

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);

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
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Gemini API request failed. Retry {RetryCount} after {DelaySeconds}s",
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
            // Load system prompt
            var systemPrompt = await LoadSystemPromptAsync();

            // Fetch active themes
            var themes = await _themeRepository.FindAsync(t => t.IsActive);
            var themeNames = themes.Select(t => t.Name).ToList();

            var prompt = type == InputType.General
                ? BuildEnhancedGeneralInputAnalysisPrompt(body, systemPrompt, themeNames)
                : BuildEnhancedInquiryInputAnalysisPrompt(body, systemPrompt, themeNames);

            var (response, usage) = await CallGeminiApiAsync(prompt, "input_analysis");
            var result = ParseAnalysisResponse(response);

            // Track cost
            var cost = CalculateGeminiCost(usage.PromptTokenCount, usage.CandidatesTokenCount);
            await _costTracking.LogRequestAsync(
                "input_analysis",
                usage.PromptTokenCount,
                usage.CandidatesTokenCount,
                (double)cost);

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

    private async Task<string> LoadSystemPromptAsync()
    {
        var promptPath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "university_prompt.txt");

        // Fallback for dev environment
        if (!File.Exists(promptPath))
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/SmartInsights.Infrastructure"));
            promptPath = Path.Combine(projectRoot, "Data", "SeedData", "university_prompt.txt");
        }

        if (File.Exists(promptPath))
        {
            return await File.ReadAllTextAsync(promptPath);
        }

        return "You are an AI assistant for a university.";
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
            // Fetch existing topics for context
            // We limit to recent active topics to keep context window manageable
            var existingTopics = await _topicRepository.FindAsync(t =>
                (departmentId == null || t.DepartmentId == departmentId) && !t.IsArchived);

            var topicNames = existingTopics
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var prompt = BuildTopicGenerationPrompt(body, topicNames);
            var (response, usage) = await CallGeminiApiAsync(prompt, "topic_generation");

            var cost = CalculateGeminiCost(usage.PromptTokenCount, usage.CandidatesTokenCount);
            await _costTracking.LogRequestAsync(
                "topic_generation",
                usage.PromptTokenCount,
                usage.CandidatesTokenCount,
                (double)cost);

            var topicName = response.Trim().Trim('"');

            // Check if the returned name matches an existing topic (case-insensitive)
            var existingTopic = existingTopics.FirstOrDefault(t =>
                t.Name.Equals(topicName, StringComparison.OrdinalIgnoreCase));

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
            var (response, usage) = await CallGeminiApiAsync(prompt, "inquiry_summary");

            var cost = CalculateGeminiCost(usage.PromptTokenCount, usage.CandidatesTokenCount);
            await _costTracking.LogRequestAsync(
                "inquiry_summary",
                usage.PromptTokenCount,
                usage.CandidatesTokenCount,
                (double)cost);

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

    public async Task<ExecutiveSummary> GenerateTopicSummaryAsync(Guid topicId, List<Input> inputs, bool bypassCache = false)
    {
        // Check for empty inputs first
        if (!inputs.Any())
        {
            return CreateEmptySummary();
        }

        var cacheKey = $"gemini_topic_summary_{topicId}_{inputs.Count}_{inputs.Max(i => i.UpdatedAt):yyyyMMddHHmmss}";

        if (!bypassCache && _cache.TryGetValue<ExecutiveSummary>(cacheKey, out var cachedSummary))
        {
            _logger.LogInformation("Using cached topic summary from Gemini");
            return cachedSummary!;
        }

        if (bypassCache)
        {
            _logger.LogInformation("Bypassing cache for topic summary generation");
        }

        try
        {
            var prompt = BuildTopicSummaryPrompt(inputs);
            var (response, usage) = await CallGeminiApiAsync(prompt, "topic_summary");

            _logger.LogInformation("Raw Gemini response for topic summary: {Response}", response);

            var cost = CalculateGeminiCost(usage.PromptTokenCount, usage.CandidatesTokenCount);
            await _costTracking.LogRequestAsync(
                "topic_summary",
                usage.PromptTokenCount,
                usage.CandidatesTokenCount,
                (double)cost);

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

    private async Task<(string response, UsageMetadata usage)> CallGeminiApiAsync(string prompt, string operationType)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = _temperature,
                    maxOutputTokens = _maxTokens
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API request failed: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

            if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
            {
                throw new InvalidOperationException("No candidates returned from Gemini API");
            }

            var text = geminiResponse.Candidates[0].Content.Parts[0].Text;
            var usage = geminiResponse.UsageMetadata ?? new UsageMetadata
            {
                PromptTokenCount = EstimateTokens(prompt),
                CandidatesTokenCount = EstimateTokens(text),
                TotalTokenCount = EstimateTokens(prompt) + EstimateTokens(text)
            };

            return (text, usage);
        });
    }

    private string BuildEnhancedGeneralInputAnalysisPrompt(string body, string systemPrompt, List<string> themes)
    {
        var themesList = string.Join("\" or \"", themes);
        if (string.IsNullOrEmpty(themesList)) themesList = "Other";

        return $@"{systemPrompt}

Analyze this student feedback and return ONLY a valid JSON object with these exact fields:

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
  ""theme"": ""{themesList}""
}}

Guidelines:
- Urgency: How quickly this needs attention (0=low, 1=urgent)
- Importance: Impact on student experience (0=minor, 1=critical)
- Clarity: How clear and specific is the feedback (0=vague, 1=very clear)
- Quality: Overall quality of feedback (0=poor, 1=excellent)
- Helpfulness: How actionable/useful is this feedback (0=not useful, 1=very useful)
- Theme: Must be one of the provided options.

Return ONLY the JSON, no additional text.";
    }

    private string BuildEnhancedInquiryInputAnalysisPrompt(string body, string systemPrompt, List<string> themes)
    {
        var themesList = string.Join("\" or \"", themes);
        if (string.IsNullOrEmpty(themesList)) themesList = "Other";

        return $@"{systemPrompt}

Analyze this student response to an inquiry and return ONLY a valid JSON object:

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
  ""theme"": ""{themesList}""
}}

Return ONLY the JSON, no additional text.";
    }

    private string BuildTopicGenerationPrompt(string body, List<string> existingTopics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Based on this student feedback, assign it to an EXISTING topic from the list below, or generate a NEW concise topic name (3-6 words) if none fit.");

        if (existingTopics.Any())
        {
            sb.AppendLine("\nExisting Topics:");
            foreach (var topic in existingTopics)
            {
                sb.AppendLine($"- {topic}");
            }
        }

        sb.AppendLine($"\nFeedback: \"{body}\"");
        sb.AppendLine("\nInstructions:");
        sb.AppendLine("1. If the feedback clearly belongs to an existing topic, return that EXACT topic name.");
        sb.AppendLine("2. If it represents a new issue, generate a new concise name.");
        sb.AppendLine("3. Return ONLY the topic name, nothing else.");
        sb.AppendLine("\nTopic name:");

        return sb.ToString();
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
  ""executiveSummaryData"": {
    ""summary"": ""A concise 2-3 paragraph summary of key findings"",
    ""keyPoints"": ""- Point 1\n- Point 2\n- Point 3 (IMPORTANT: Return as a single string with newlines, NOT a JSON array)""
  },
  ""suggestedPrioritizedActions"": [
    {
      ""action"": ""Action description"",
      ""impact"": ""Expected impact"",
      ""challenges"": ""Potential challenges"",
      ""responseCount"": 10,
      ""supportingReasoning"": ""Why this action""
    }
  ]
}

CRITICAL INSTRUCTIONS FOR ACTIONS:
1. Generate a MAXIMUM of 5 suggested actions.
2. Actions must be HIGHLY ACTIONABLE and SPECIFIC. Avoid generic advice like ""Improve communication"". Instead, say ""Implement weekly status emails"".
3. Act as an expert consultant. Focus on high-impact, feasible interventions.
4. Prioritize actions that address the most urgent or widespread issues.

Return ONLY the JSON, no additional text. Ensure executiveSummaryData values are STRINGS, not arrays.");

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
Create an executive summary in JSON format:
{
  ""topics"": [""topic1"", ""topic2"", ""topic3""],
  ""executiveSummaryData"": {
    ""summary"": ""A concise 2-3 paragraph summary of key findings"",
    ""keyPoints"": ""- Point 1\n- Point 2\n- Point 3 (IMPORTANT: Return as a single string with newlines, NOT a JSON array)""
  },
  ""suggestedPrioritizedActions"": [
    {
      ""action"": ""Action description"",
      ""impact"": ""Expected impact"",
      ""challenges"": ""Potential challenges"",
      ""responseCount"": 10,
      ""supportingReasoning"": ""Why this action""
    }
  ]
}

CRITICAL INSTRUCTIONS FOR ACTIONS:
1. Generate a MAXIMUM of 5 suggested actions.
2. Actions must be HIGHLY ACTIONABLE and SPECIFIC. Avoid generic advice.
3. Act as an expert consultant. Focus on high-impact, feasible interventions.
4. Prioritize actions that address the most urgent or widespread issues.

Return ONLY the JSON, no additional text. Ensure executiveSummaryData values are STRINGS, not arrays.");

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
            ExecutiveSummaryData = new Dictionary<string, string>
            {
                { "summary", "No responses have been submitted yet." }
            },
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
        // Gemini Pro pricing (as of 2024):
        // Input: $0.00025 / 1K tokens
        // Output: $0.0005 / 1K tokens
        decimal inputCost = (promptTokens / 1000m) * 0.00025m;
        decimal outputCost = (completionTokens / 1000m) * 0.0005m;
        return inputCost + outputCost;
    }

    // Gemini API Response Models
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata? UsageMetadata { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; } = new();
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }
}
