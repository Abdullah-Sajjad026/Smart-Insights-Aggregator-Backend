namespace SmartInsights.Application.DTOs.Users;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = "Student";
    public Guid? DepartmentId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? SemesterId { get; set; }
}
