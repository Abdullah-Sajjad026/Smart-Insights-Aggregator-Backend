using SmartInsights.Domain.Entities;

namespace SmartInsights.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    Guid? ValidateToken(string token);
}
