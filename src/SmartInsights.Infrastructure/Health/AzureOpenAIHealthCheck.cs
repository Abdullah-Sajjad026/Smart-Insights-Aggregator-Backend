using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.Infrastructure.Health;

/// <summary>
/// Health check for Azure OpenAI service availability
/// </summary>
public class AzureOpenAIHealthCheck : IHealthCheck
{
    private readonly IAIService _aiService;
    private readonly ILogger<AzureOpenAIHealthCheck> _logger;

    public AzureOpenAIHealthCheck(
        IAIService aiService,
        ILogger<AzureOpenAIHealthCheck> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a simple analysis with minimal tokens to test connectivity
            var testInput = "Test";
            var result = await _aiService.AnalyzeInputAsync(testInput, Domain.Enums.InputType.General);

            if (result != null)
            {
                return HealthCheckResult.Healthy(
                    "Azure OpenAI service is responding normally",
                    new Dictionary<string, object>
                    {
                        { "service", "Azure OpenAI" },
                        { "status", "Connected" }
                    });
            }

            return HealthCheckResult.Degraded(
                "Azure OpenAI service returned null response",
                data: new Dictionary<string, object>
                {
                    { "service", "Azure OpenAI" },
                    { "status", "Degraded" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed");

            return HealthCheckResult.Unhealthy(
                "Azure OpenAI service is not available",
                ex,
                new Dictionary<string, object>
                {
                    { "service", "Azure OpenAI" },
                    { "status", "Unhealthy" },
                    { "error", ex.Message }
                });
        }
    }
}
