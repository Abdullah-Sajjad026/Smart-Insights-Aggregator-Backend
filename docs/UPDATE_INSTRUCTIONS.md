# Manual Updates Required

## 1. Update InputService.cs

In `src/SmartInsights.Application/Services/InputService.cs`:

### Add IAIService dependency to constructor:

```csharp
private readonly IAIService _aiService;

public InputService(
    IRepository<Input> inputRepository,
    IRepository<InputReply> inputReplyRepository,
    IRepository<User> userRepository,
    IRepository<Inquiry> inquiryRepository,
    IRepository<Topic> topicRepository,
    IRepository<Theme> themeRepository,
    IAIService aiService) // ADD THIS
{
    _inputRepository = inputRepository;
    _inputReplyRepository = inputReplyRepository;
    _userRepository = userRepository;
    _inquiryRepository = inquiryRepository;
    _topicRepository = topicRepository;
    _themeRepository = themeRepository;
    _aiService = aiService; // ADD THIS
}
```

### Replace the CreateAsync method with AI-enhanced version:

See the implementation in `InputServiceWithAI.cs` - replace the entire CreateAsync method with the version that includes AI processing.

## 2. Add Required NuGet Package

The package has already been added to SmartInsights.Infrastructure.csproj:
- Azure.AI.OpenAI (Version 1.0.0-beta.12)

## 3. Update Program.cs

Add AI Service registration (already added):
```csharp
builder.Services.AddScoped<IAIService, AzureOpenAIService>();
```

## 4. Configure Azure OpenAI Settings

Update `appsettings.json` with your actual Azure OpenAI credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

## 5. Background Job for Summary Generation

After Hangfire is set up, create a background job that:
1. Monitors inquiries with 10+ processed inputs but no summary
2. Monitors topics with 10+ processed inputs but no summary
3. Calls `IAIService.GenerateInquirySummaryAsync()` or `IAIService.GenerateTopicSummaryAsync()`
4. Updates the inquiry/topic with the generated summary

## Testing AI Integration

1. Submit test feedback through `/api/inputs`
2. Check that the input status changes: Pending → Processing → Processed
3. Verify AI analysis results are populated (sentiment, tone, scores)
4. For general inputs, verify a topic is created or existing topic is matched
5. Submit 10+ inputs to an inquiry/topic and trigger summary generation
