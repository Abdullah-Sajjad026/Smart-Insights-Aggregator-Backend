using Microsoft.EntityFrameworkCore;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Users;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<Program> _programRepository;
    private readonly IRepository<Semester> _semesterRepository;
    private readonly IPasswordService _passwordService;

    public UserService(
        IRepository<User> userRepository,
        IRepository<Department> departmentRepository,
        IRepository<Program> programRepository,
        IRepository<Semester> semesterRepository,
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _programRepository = programRepository;
        _semesterRepository = semesterRepository;
        _passwordService = passwordService;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id, 
            u => u.Department!,
            u => u.Program!,
            u => u.Semester!);

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<PaginatedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20, string? role = null, string? status = null)
    {
        var users = await _userRepository.GetAllAsync(
            u => u.Department!,
            u => u.Program!,
            u => u.Semester!);

        var query = users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(role) && Enum.TryParse<Role>(role, out var roleEnum))
        {
            query = query.Where(u => u.Role == roleEnum);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
        {
            query = query.Where(u => u.Status == statusEnum);
        }

        var totalCount = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

        return new PaginatedResult<UserDto>
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        // Check if email already exists
        var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.Role, out var role))
        {
            throw new ArgumentException("Invalid role");
        }

        // For students, validate required fields
        if (role == Role.Student)
        {
            if (!request.DepartmentId.HasValue || !request.ProgramId.HasValue || !request.SemesterId.HasValue)
            {
                throw new ArgumentException("Department, Program, and Semester are required for students");
            }

            // Validate that department, program, and semester exist
            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value);
            if (department == null)
                throw new ArgumentException("Department not found");

            var program = await _programRepository.GetByIdAsync(request.ProgramId.Value);
            if (program == null)
                throw new ArgumentException("Program not found");

            var semester = await _semesterRepository.GetByIdAsync(request.SemesterId.Value);
            if (semester == null)
                throw new ArgumentException("Semester not found");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Role = role,
            Status = UserStatus.Active,
            DepartmentId = request.DepartmentId,
            ProgramId = request.ProgramId,
            SemesterId = request.SemesterId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<UserStatus>(request.Status, out var status))
            user.Status = status;

        if (request.DepartmentId.HasValue)
            user.DepartmentId = request.DepartmentId;

        if (request.ProgramId.HasValue)
            user.ProgramId = request.ProgramId;

        if (request.SemesterId.HasValue)
            user.SemesterId = request.SemesterId;

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        await _userRepository.DeleteAsync(user);
    }

    public async Task<List<UserDto>> ImportFromCsvAsync(Stream csvStream)
    {
        var users = new List<User>();
        
        using var reader = new StreamReader(csvStream);
        
        // Skip header
        await reader.ReadLineAsync();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');
            
            if (values.Length < 7) continue; // Email, FirstName, LastName, Password, Role, Department, Program, Semester

            var email = values[0].Trim();
            
            // Skip if user already exists
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null) continue;

            if (!Enum.TryParse<Role>(values[4].Trim(), out var role))
                continue;

            Guid? departmentId = null, programId = null, semesterId = null;

            if (role == Role.Student)
            {
                // Find department by name
                var department = (await _departmentRepository.GetAllAsync())
                    .FirstOrDefault(d => d.Name.Equals(values[5].Trim(), StringComparison.OrdinalIgnoreCase));
                if (department == null) continue;
                departmentId = department.Id;

                // Find program by name
                var program = (await _programRepository.GetAllAsync())
                    .FirstOrDefault(p => p.Name.Equals(values[6].Trim(), StringComparison.OrdinalIgnoreCase));
                if (program == null) continue;
                programId = program.Id;

                // Find semester by value
                var semester = (await _semesterRepository.GetAllAsync())
                    .FirstOrDefault(s => s.Value.Equals(values[7].Trim(), StringComparison.OrdinalIgnoreCase));
                if (semester == null) continue;
                semesterId = semester.Id;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = values[1].Trim(),
                LastName = values[2].Trim(),
                PasswordHash = _passwordService.HashPassword(values[3].Trim()),
                Role = role,
                Status = UserStatus.Active,
                DepartmentId = departmentId,
                ProgramId = programId,
                SemesterId = semesterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            users.Add(user);
        }

        if (users.Any())
        {
            await _userRepository.AddRangeAsync(users);
        }

        return users.Select(MapToDto).ToList();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _userRepository.CountAsync();
    }

    public async Task<Dictionary<string, int>> GetCountByRoleAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.GroupBy(u => u.Role.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            Department = user.Department?.Name,
            Program = user.Program?.Name,
            Semester = user.Semester?.Value,
            CreatedAt = user.CreatedAt
        };
    }
}
