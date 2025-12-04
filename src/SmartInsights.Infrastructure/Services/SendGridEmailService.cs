using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartInsights.Application.DTOs.Email;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["Email:SendGridApiKey"] ?? throw new InvalidOperationException("SendGrid API Key not configured");
        _fromEmail = configuration["Email:From"] ?? "noreply@smartinsights.com";
        _fromName = configuration["Email:FromName"] ?? "SmartInsights";
    }

    public async Task SendEmailAsync(EmailMessage emailMessage)
    {
        try
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(emailMessage.To);
            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                emailMessage.Subject,
                emailMessage.IsHtml ? null : emailMessage.Body,
                emailMessage.IsHtml ? emailMessage.Body : null
            );

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
                response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Email sent successfully to {To} via SendGrid", emailMessage.To);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {To}. Status: {Status}, Body: {Body}",
                    emailMessage.To, response.StatusCode, errorBody);
                throw new InvalidOperationException($"Failed to send email. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SendGrid", emailMessage.To);
            throw;
        }
    }

    public async Task SendInvitationEmailAsync(string toEmail, string firstName, string invitationToken, string invitationLink)
    {
        var subject = "Welcome to SmartInsights - Complete Your Registration";
        var body = GetInvitationEmailTemplate(firstName, invitationLink);

        var emailMessage = new EmailMessage
        {
            To = toEmail,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        await SendEmailAsync(emailMessage);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetToken, string resetLink)
    {
        var subject = "SmartInsights - Password Reset Request";
        var body = GetPasswordResetEmailTemplate(firstName, resetLink);

        var emailMessage = new EmailMessage
        {
            To = toEmail,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        await SendEmailAsync(emailMessage);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var subject = "Welcome to SmartInsights!";
        var body = GetWelcomeEmailTemplate(firstName);

        var emailMessage = new EmailMessage
        {
            To = toEmail,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        await SendEmailAsync(emailMessage);
    }

    public async Task SendPasswordChangedEmailAsync(string toEmail, string firstName)
    {
        var subject = "SmartInsights - Password Changed Successfully";
        var body = GetPasswordChangedEmailTemplate(firstName);

        var emailMessage = new EmailMessage
        {
            To = toEmail,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        await SendEmailAsync(emailMessage);
    }

    private string GetInvitationEmailTemplate(string firstName, string invitationLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 5px; margin-top: 20px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to SmartInsights!</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>You have been invited to join SmartInsights, our intelligent feedback management system.</p>
            <p>To complete your registration and set your password, please click the button below:</p>
            <p style='text-align: center;'>
                <a href='{invitationLink}' class='button'>Accept Invitation & Set Password</a>
            </p>
            <p style='font-size: 12px; color: #666; margin-top: 30px;'>
                If the button doesn't work, copy and paste this link into your browser:<br>
                {invitationLink}
            </p>
            <p style='font-size: 12px; color: #666; margin-top: 20px;'>
                This invitation link will expire in 7 days. If you didn't expect this invitation, you can safely ignore this email.
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 SmartInsights. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetPasswordResetEmailTemplate(string firstName, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 5px; margin-top: 20px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>We received a request to reset your password for your SmartInsights account.</p>
            <p>To reset your password, please click the button below:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <div class='warning'>
                <strong>Security Notice:</strong> If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
            </div>
            <p style='font-size: 12px; color: #666; margin-top: 30px;'>
                If the button doesn't work, copy and paste this link into your browser:<br>
                {resetLink}
            </p>
            <p style='font-size: 12px; color: #666;'>
                This password reset link will expire in 1 hour.
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 SmartInsights. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetWelcomeEmailTemplate(string firstName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to SmartInsights!</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>Welcome to SmartInsights! Your account has been successfully activated.</p>
            <p>SmartInsights helps you provide and manage feedback efficiently using AI-powered insights.</p>
            <p>If you have any questions or need assistance, feel free to reach out to our support team.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 SmartInsights. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetPasswordChangedEmailTemplate(string firstName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .info {{ background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 10px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Changed</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>Your SmartInsights account password has been changed successfully.</p>
            <div class='info'>
                <strong>Security Notice:</strong> If you didn't make this change, please contact our support team immediately.
            </div>
        </div>
        <div class='footer'>
            <p>&copy; 2024 SmartInsights. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
