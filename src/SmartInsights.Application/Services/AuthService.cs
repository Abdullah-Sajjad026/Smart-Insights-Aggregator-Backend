using SmartInsights.Application.DTOs.Auth;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("User account is not active");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var token = _jwtService.GenerateToken(user);
        var expiryMinutes = 1440; // Should come from config

        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var userId = _jwtService.ValidateToken(token);
        if (userId == null) return false;

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return user != null && user.Status == UserStatus.Active;
    }
}
