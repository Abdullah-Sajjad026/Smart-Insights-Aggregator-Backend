using SmartInsights.Application.DTOs.Email;

namespace SmartInsights.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage emailMessage);
    Task SendInvitationEmailAsync(string toEmail, string firstName, string invitationToken, string invitationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, string resetLink);
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
    Task SendPasswordChangedEmailAsync(string toEmail, string firstName);
}
