using Hangfire.Dashboard;

namespace SmartInsights.API.Infrastructure;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In development: allows all access
/// In production: should check for admin role
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, you should check authentication:
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.IsInRole("Admin");

        return true; // Allow access in development
    }
}
