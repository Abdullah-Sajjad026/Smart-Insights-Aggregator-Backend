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
    private readonly IEmailService _emailService;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordService passwordService,
        IJwtService jwtService,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _emailService = emailService;
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
            UserId = user.Id,
            Token = token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
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

    public async Task<LoginResponse> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        // Validate passwords match
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match");
        }

        // Validate password strength (basic validation)
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long");
        }

        // Find user by invitation token
        var user = await _userRepository.FirstOrDefaultAsync(u =>
            u.InvitationToken == request.InvitationToken);

        if (user == null)
        {
            throw new ArgumentException("Invalid invitation token");
        }

        // Check if invitation has expired
        if (user.InvitationTokenExpiresAt == null || user.InvitationTokenExpiresAt < DateTime.UtcNow)
        {
            throw new ArgumentException("Invitation token has expired");
        }

        // Check if invitation has already been accepted
        if (user.InvitationAcceptedAt != null)
        {
            throw new ArgumentException("Invitation has already been accepted");
        }

        // Set password
        user.PasswordHash = _passwordService.HashPassword(request.Password);

        // Mark invitation as accepted
        user.InvitationAcceptedAt = DateTime.UtcNow;
        user.InvitationToken = null;
        user.InvitationTokenExpiresAt = null;

        // Mark email as verified
        user.EmailVerified = true;

        // Activate user
        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

        // Generate JWT token and log the user in
        var token = _jwtService.GenerateToken(user);
        var expiryMinutes = 1440;

        return new LoginResponse
        {
            UserId = user.Id,
            Token = token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }
}
