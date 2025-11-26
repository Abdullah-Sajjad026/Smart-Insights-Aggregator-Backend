# AI Provider Configuration Guide

This project uses the **Strategy Pattern** to support multiple AI providers. You can easily switch between Google Gemini and Azure OpenAI without changing code.

## Supported Providers

1. **Google Gemini** (Default) - Free tier available, easy to get started
   - Uses Gemini REST API directly (no external packages required)
2. **Azure OpenAI** - Enterprise-grade, requires Azure subscription

## Quick Start with Google Gemini (Recommended for Development)

### 1. Get Your Gemini API Key

1. Visit [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy your API key

### 2. Configure the Application

**Option A: Using appsettings.json**
```json
{
  "AI": {
    "Provider": "gemini"
  },
  "Gemini": {
    "ApiKey": "your-gemini-api-key-here",
    "Model": "gemini-1.5-flash-latest",
    "Temperature": "0.7",
    "MaxTokens": "2000",
    "MaxRetries": "3",
    "CacheExpirationHours": "24"
  }
}
```

**Option B: Using Environment Variables** (Recommended for production)
```bash
export AI__Provider="gemini"
export Gemini__ApiKey="your-gemini-api-key"
export Gemini__Model="gemini-1.5-flash-latest"
```

> **Note:** We use `gemini-1.5-flash-latest` instead of other models because:
> - **gemini-1.5-flash-latest**: 15 requests/minute (Free tier) - **Recommended**
> - **gemini-2.5-pro**: Only 2 requests/minute (Free tier) - Too restrictive
> - **gemini-pro**: Older model, deprecated

**Option C: Using .NET User Secrets** (Recommended for local development)
```bash
cd src/SmartInsights.API
dotnet user-secrets set "AI:Provider" "gemini"
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key"
```

### 3. Run the Application

```bash
dotnet run --project src/SmartInsights.API
```

You should see: `[INF] Using Google Gemini as AI provider`

## Switching to Azure OpenAI

### 1. Prerequisites

- Azure subscription
- Azure OpenAI resource created
- GPT-4 or GPT-3.5-turbo deployment

### 2. Get Azure OpenAI Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Copy:
   - **Endpoint**: `https://your-resource.openai.azure.com/`
   - **API Key**: From "Keys and Endpoint" section
   - **Deployment Name**: Your model deployment name

### 3. Configure the Application

```bash
dotnet user-secrets set "AI:Provider" "azureopenai"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"
```

### 4. Run the Application

```bash
dotnet run --project src/SmartInsights.API
```

You should see: `[INF] Using Azure OpenAI as AI provider`

## Configuration Reference

### Gemini Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `Gemini:ApiKey` | Your Gemini API key (required) | - |
| `Gemini:Model` | Model to use | `gemini-1.5-flash-latest` |
| `Gemini:Temperature` | Creativity (0.0-1.0) | `0.7` |
| `Gemini:MaxTokens` | Max response length | `2000` |
| `Gemini:MaxRetries` | Retry attempts on failure | `3` |
| `Gemini:CacheExpirationHours` | Cache duration | `24` |

### Azure OpenAI Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `AzureOpenAI:Endpoint` | Azure OpenAI endpoint (required) | - |
| `AzureOpenAI:ApiKey` | Azure API key (required) | - |
| `AzureOpenAI:DeploymentName` | Model deployment name (required) | - |
| `AzureOpenAI:Temperature` | Creativity (0.0-1.0) | `0.7` |
| `AzureOpenAI:MaxTokens` | Max response length | `2000` |
| `AzureOpenAI:MaxRetries` | Retry attempts on failure | `3` |
| `AzureOpenAI:CacheExpirationHours` | Cache duration | `24` |

## Architecture: Strategy Pattern

The implementation uses the Strategy Pattern for flexibility:

```
IAIService (Interface)
    ├── GeminiAIService (Strategy 1)
    └── ImprovedAzureOpenAIService (Strategy 2)
```

### Key Features

✅ **Easy Provider Switching**: Change one config value
✅ **No Code Changes**: Switch providers without recompiling
✅ **Consistent Interface**: All providers implement `IAIService`
✅ **Future-Proof**: Easy to add new providers (Claude, Llama, etc.)
✅ **Caching**: Response caching to reduce costs
✅ **Retry Logic**: Automatic retries with exponential backoff
✅ **Cost Tracking**: Monitor API usage and costs

## Cost Comparison (as of 2024)

### Google Gemini Pro
- **Free Tier**: 60 requests/minute
- **Pricing**: $0.00025/1K input tokens, $0.0005/1K output tokens
- **Best For**: Development, testing, low-volume production

### Azure OpenAI (GPT-4)
- **No Free Tier**
- **Pricing**: ~$0.03/1K input tokens, ~$0.06/1K output tokens
- **Best For**: Enterprise, high-quality production workloads

## Adding a New AI Provider

To add a new provider (e.g., Claude, Llama):

1. Create a new service class implementing `IAIService`:
   ```csharp
   public class ClaudeAIService : IAIService
   {
       // Implement interface methods
   }
   ```

2. Add configuration in `appsettings.json`:
   ```json
   {
     "Claude": {
       "ApiKey": "your-api-key"
     }
   }
   ```

3. Register in `Program.cs`:
   ```csharp
   else if (aiProvider == "claude")
   {
       builder.Services.AddScoped<IAIService, ClaudeAIService>();
   }
   ```

## Troubleshooting

### "Gemini API key not configured"
- Check your configuration is set correctly
- Verify environment variable names use double underscores: `Gemini__ApiKey`

### "Rate limit exceeded"
- Gemini free tier: 60 requests/minute
- Wait or upgrade to paid tier

### "Invalid API key"
- Regenerate API key in Google AI Studio
- Check for extra spaces in configuration

### Provider not switching
- Restart the application after config changes
- Check startup logs for which provider is being used

## Testing AI Functionality

1. **Submit test feedback**: Use the `/api/inputs` endpoint
2. **Check Hangfire**: Visit `/hangfire` to see AI processing jobs
3. **View results**: Check the input's AI-generated fields (sentiment, urgency, etc.)
4. **Monitor costs**: Use `/api/monitoring/ai/usage` endpoint

## Best Practices

1. **Use User Secrets for local development**: Never commit API keys
2. **Use Environment Variables for production**: Set in your hosting environment
3. **Monitor costs**: Check `/api/monitoring/ai/cost` regularly
4. **Cache aggressively**: Responses are cached for 24 hours by default
5. **Start with Gemini**: Free tier is perfect for development and testing
