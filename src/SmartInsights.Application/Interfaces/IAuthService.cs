using SmartInsights.Application.DTOs.Auth;

namespace SmartInsights.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Task<LoginResponse> AcceptInvitationAsync(AcceptInvitationRequest request);
}
