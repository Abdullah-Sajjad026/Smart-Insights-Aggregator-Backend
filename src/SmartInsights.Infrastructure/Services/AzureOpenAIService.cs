using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Infrastructure.Services;

public class AzureOpenAIService : IAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Theme> _themeRepository;

    public AzureOpenAIService(
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger,
        IRepository<Topic> topicRepository,
        IRepository<Theme> themeRepository)
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
    }

    public async Task<InputAnalysisResult> AnalyzeInputAsync(string body, InputType type)
    {
        try
        {
            var prompt = type == InputType.General
                ? BuildGeneralInputAnalysisPrompt(body)
                : BuildInquiryInputAnalysisPrompt(body);

            var response = await CallOpenAIAsync(prompt);
            return ParseAnalysisResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing input");
            
            // Return default values on error
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
            // Generate topic name from feedback
            var prompt = BuildTopicGenerationPrompt(body);
            var response = await CallOpenAIAsync(prompt);
            var topicName = response.Trim().Trim('"');

            // Limit to 100 characters
            if (topicName.Length > 100)
                topicName = topicName.Substring(0, 97) + "...";

            // Check if similar topic exists for this department
            var existingTopics = await _topicRepository.FindAsync(t => 
                t.DepartmentId == departmentId || t.DepartmentId == null);

            var matchingTopic = existingTopics.FirstOrDefault(t => 
                t.Name.Equals(topicName, StringComparison.OrdinalIgnoreCase) ||
                IsSimilarTopic(t.Name, topicName));

            if (matchingTopic != null)
            {
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
            return newTopic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topic");
            
            // Return a default topic
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
        try
        {
            if (!inputs.Any())
            {
                return CreateEmptySummary();
            }

            var prompt = BuildInquirySummaryPrompt(inputs);
            var response = await CallOpenAIAsync(prompt);
            return ParseExecutiveSummary(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inquiry summary for {InquiryId}", inquiryId);
            return CreateEmptySummary();
        }
    }

    public async Task<ExecutiveSummary> GenerateTopicSummaryAsync(Guid topicId, List<Input> inputs)
    {
        try
        {
            if (!inputs.Any())
            {
                return CreateEmptySummary();
            }

            var prompt = BuildTopicSummaryPrompt(inputs);
            var response = await CallOpenAIAsync(prompt);
            return ParseExecutiveSummary(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topic summary for {TopicId}", topicId);
            return CreateEmptySummary();
        }
    }

    // Private helper methods
    private async Task<string> CallOpenAIAsync(string prompt, int maxTokens = 2000)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage("You are an AI assistant specialized in analyzing student feedback for a university. Provide structured, actionable insights."),
                new ChatRequestUserMessage(prompt)
            },
            MaxTokens = maxTokens,
            Temperature = 0.7f
        };

        var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }

    private string BuildGeneralInputAnalysisPrompt(string body)
    {
        return $@"Analyze this student feedback and provide a JSON response with the following structure:
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

Feedback: ""{body}""

Rating Guidelines:
- Urgency: How time-sensitive is this issue?
- Importance: How many students might this affect?
- Clarity: How well-articulated is the feedback?
- Quality: How actionable and constructive is it?
- Helpfulness: How useful is this for decision-making?

Provide only the JSON, no additional text.";
    }

    private string BuildInquiryInputAnalysisPrompt(string body)
    {
        return $@"Analyze this inquiry response and provide a JSON response with the following structure:
{{
    ""sentiment"": ""Positive|Neutral|Negative"",
    ""tone"": ""Positive|Neutral|Negative"",
    ""urgency"": 0.0 to 1.0,
    ""importance"": 0.0 to 1.0,
    ""clarity"": 0.0 to 1.0,
    ""quality"": 0.0 to 1.0,
    ""helpfulness"": 0.0 to 1.0
}}

Response: ""{body}""

Provide only the JSON, no additional text.";
    }

    private string BuildTopicGenerationPrompt(string body)
    {
        return $@"Generate a concise topic name (max 5 words) that categorizes this feedback:

Feedback: ""{body}""

Examples:
- ""Library WiFi Connectivity Issues""
- ""Cafeteria Food Quality""
- ""Exam Scheduling Conflicts""

Provide only the topic name, nothing else.";
    }

    private string BuildInquirySummaryPrompt(List<Input> inputs)
    {
        var feedbackList = new StringBuilder();
        foreach (var input in inputs.Take(100)) // Limit to avoid token limits
        {
            feedbackList.AppendLine($"- {input.Body}");
        }

        return $@"Analyze these {inputs.Count} student responses and generate an executive summary in JSON format:

{{
    ""topics"": [""topic1"", ""topic2"", ""topic3""],
    ""executiveSummaryData"": {{
        ""headlineInsight"": ""One-line key finding"",
        ""responseMix"": ""{inputs.Count} responses: X positive, Y neutral, Z negative"",
        ""keyTakeaways"": ""2-3 paragraph detailed analysis"",
        ""risks"": ""What could go wrong if not addressed"",
        ""opportunities"": ""What could be improved""
    }},
    ""suggestedPrioritizedActions"": [
        {{
            ""action"": ""Specific action to take"",
            ""impact"": ""HIGH|MEDIUM|LOW"",
            ""challenges"": ""Implementation challenges"",
            ""responseCount"": number_of_responses,
            ""supportingReasoning"": ""Why this action is important""
        }}
    ]
}}

Responses:
{feedbackList}

Provide only valid JSON, no additional text.";
    }

    private string BuildTopicSummaryPrompt(List<Input> inputs)
    {
        return BuildInquirySummaryPrompt(inputs); // Same format
    }

    private InputAnalysisResult ParseAnalysisResponse(string jsonResponse)
    {
        try
        {
            // Try to extract JSON if wrapped in markdown or other text
            var startIndex = jsonResponse.IndexOf('{');
            var endIndex = jsonResponse.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                jsonResponse = jsonResponse.Substring(startIndex, endIndex - startIndex + 1);
            }

            var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var sentiment = Enum.Parse<Sentiment>(root.GetProperty("sentiment").GetString() ?? "Neutral", true);
            var tone = Enum.Parse<Tone>(root.GetProperty("tone").GetString() ?? "Neutral", true);
            var urgency = root.GetProperty("urgency").GetDouble();
            var importance = root.GetProperty("importance").GetDouble();
            var clarity = root.GetProperty("clarity").GetDouble();
            var quality = root.GetProperty("quality").GetDouble();
            var helpfulness = root.GetProperty("helpfulness").GetDouble();

            var score = (urgency + importance + clarity + quality + helpfulness) / 5.0;
            var severity = score >= 0.75 ? 3 : score >= 0.5 ? 2 : 1;

            var theme = root.TryGetProperty("theme", out var themeElement) 
                ? themeElement.GetString() ?? "General" 
                : "General";

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
            _logger.LogError(ex, "Error parsing AI response: {Response}", jsonResponse);
            throw;
        }
    }

    private ExecutiveSummary ParseExecutiveSummary(string jsonResponse)
    {
        try
        {
            // Try to extract JSON if wrapped in markdown or other text
            var startIndex = jsonResponse.IndexOf('{');
            var endIndex = jsonResponse.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                jsonResponse = jsonResponse.Substring(startIndex, endIndex - startIndex + 1);
            }

            return JsonSerializer.Deserialize<ExecutiveSummary>(jsonResponse) ?? CreateEmptySummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing executive summary: {Response}", jsonResponse);
            return CreateEmptySummary();
        }
    }

    private bool IsSimilarTopic(string topic1, string topic2)
    {
        // Simple similarity check - can be enhanced with Levenshtein distance
        var words1 = topic1.ToLower().Split(' ');
        var words2 = topic2.ToLower().Split(' ');
        
        var commonWords = words1.Intersect(words2).Count();
        var totalWords = Math.Max(words1.Length, words2.Length);
        
        return (double)commonWords / totalWords >= 0.6; // 60% similarity threshold
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
