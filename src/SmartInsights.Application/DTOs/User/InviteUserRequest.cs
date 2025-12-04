namespace SmartInsights.Application.DTOs.User;

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";

    // Student-specific fields
    public Guid? DepartmentId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? SemesterId { get; set; }
}
