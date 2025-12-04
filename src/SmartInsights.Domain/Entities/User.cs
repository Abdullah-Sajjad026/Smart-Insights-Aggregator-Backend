
using SmartInsights.Domain.Enums;
using SmartInsights.Domain.Common;

namespace SmartInsights.Domain.Entities;

public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Student;
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Email verification fields
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    // Invitation fields
    public string? InvitationToken { get; set; }
    public DateTime? InvitationTokenExpiresAt { get; set; }
    public DateTime? InvitationAcceptedAt { get; set; }
    public bool IsInvitationPending => InvitationToken != null &&
                                        InvitationTokenExpiresAt > DateTime.UtcNow &&
                                        InvitationAcceptedAt == null;

    // Password reset fields
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Student-specific fields (nullable for admins)
    public Guid? DepartmentId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? SemesterId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Department? Department { get; set; }
    public Program? Program { get; set; }
    public Semester? Semester { get; set; }
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
    public ICollection<Inquiry> CreatedInquiries { get; set; } = new List<Inquiry>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
