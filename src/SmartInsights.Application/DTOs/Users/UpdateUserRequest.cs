namespace SmartInsights.Application.DTOs.Users;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Status { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? SemesterId { get; set; }
}
