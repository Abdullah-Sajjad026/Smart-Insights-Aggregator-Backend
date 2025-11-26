using System.Text.Json;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inquiries;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Application.Services;

// Topic Service
public interface ITopicService
{
    Task<TopicDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<TopicDto>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<TopicDto>> GetByDepartmentAsync(Guid departmentId);
}

public class TopicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int InputCount { get; set; }
    public ExecutiveSummaryDto? AiSummary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TopicService : ITopicService
{
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Input> _inputRepository;

    public TopicService(IRepository<Topic> topicRepository, IRepository<Input> inputRepository)
    {
        _topicRepository = topicRepository;
        _inputRepository = inputRepository;
    }

    public async Task<TopicDto?> GetByIdAsync(Guid id)
    {
        var topic = await _topicRepository.GetByIdAsync(id, t => t.Department!);
        if (topic == null) return null;

        var inputCount = await _inputRepository.CountAsync(i => i.TopicId == id);

        ExecutiveSummaryDto? summaryDto = null;
        if (!string.IsNullOrEmpty(topic.Summary))
        {
            try
            {
                var summary = JsonSerializer.Deserialize<ExecutiveSummary>(topic.Summary);
                if (summary != null)
                {
                    summaryDto = new ExecutiveSummaryDto
                    {
                        Topics = summary.Topics,
                        ExecutiveSummaryData = summary.ExecutiveSummaryData,
                        SuggestedPrioritizedActions = summary.SuggestedPrioritizedActions.Select(a => new SuggestedActionDto
                        {
                            Action = a.Action,
                            Impact = a.Impact,
                            Challenges = a.Challenges,
                            ResponseCount = a.ResponseCount,
                            SupportingReasoning = a.SupportingReasoning
                        }).ToList(),
                        GeneratedAt = topic.SummaryGeneratedAt ?? DateTime.UtcNow
                    };
                }
            }
            catch { }
        }

        return new TopicDto
        {
            Id = topic.Id,
            Name = topic.Name,
            Department = topic.Department?.Name,
            InputCount = inputCount,
            AiSummary = summaryDto,
            CreatedAt = topic.CreatedAt
        };
    }

    public async Task<PaginatedResult<TopicDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var topics = await _topicRepository.GetAllAsync(t => t.Department!);
        
        var dtos = new List<TopicDto>();
        foreach (var topic in topics)
        {
            var dto = await GetByIdAsync(topic.Id);
            if (dto != null) dtos.Add(dto);
        }

        var orderedDtos = dtos.OrderByDescending(t => t.InputCount).ToList();
        var totalCount = orderedDtos.Count;
        var items = orderedDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedResult<TopicDto>
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<List<TopicDto>> GetByDepartmentAsync(Guid departmentId)
    {
        var topics = await _topicRepository.FindAsync(t => t.DepartmentId == departmentId, t => t.Department!);
        
        var dtos = new List<TopicDto>();
        foreach (var topic in topics)
        {
            var dto = await GetByIdAsync(topic.Id);
            if (dto != null) dtos.Add(dto);
        }

        return dtos.OrderByDescending(t => t.InputCount).ToList();
    }
}

// Department Service
public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?> GetByIdAsync(Guid id);
    Task<DepartmentDto> CreateAsync(string name, string? description);
    Task<DepartmentDto> UpdateAsync(Guid id, string? name, string? description);
    Task DeleteAsync(Guid id);
}

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int TopicCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DepartmentService : IDepartmentService
{
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Topic> _topicRepository;

    public DepartmentService(
        IRepository<Department> departmentRepository,
        IRepository<User> userRepository,
        IRepository<Topic> topicRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _topicRepository = topicRepository;
    }

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        var dtos = new List<DepartmentDto>();

        foreach (var dept in departments)
        {
            var userCount = await _userRepository.CountAsync(u => u.DepartmentId == dept.Id);
            var topicCount = await _topicRepository.CountAsync(t => t.DepartmentId == dept.Id);

            dtos.Add(new DepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Description = dept.Description,
                UserCount = userCount,
                TopicCount = topicCount,
                CreatedAt = dept.CreatedAt
            });
        }

        return dtos.OrderBy(d => d.Name).ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var dept = await _departmentRepository.GetByIdAsync(id);
        if (dept == null) return null;

        var userCount = await _userRepository.CountAsync(u => u.DepartmentId == dept.Id);
        var topicCount = await _topicRepository.CountAsync(t => t.DepartmentId == dept.Id);

        return new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Description = dept.Description,
            UserCount = userCount,
            TopicCount = topicCount,
            CreatedAt = dept.CreatedAt
        };
    }

    public async Task<DepartmentDto> CreateAsync(string name, string? description)
    {
        var existing = await _departmentRepository.FirstOrDefaultAsync(d => d.Name == name);
        if (existing != null)
            throw new InvalidOperationException("Department with this name already exists");

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department);

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            UserCount = 0,
            TopicCount = 0,
            CreatedAt = department.CreatedAt
        };
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, string? name, string? description)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
            throw new KeyNotFoundException("Department not found");

        if (!string.IsNullOrEmpty(name))
            department.Name = name;

        if (description != null)
            department.Description = description;

        await _departmentRepository.UpdateAsync(department);

        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
            throw new KeyNotFoundException("Department not found");

        await _departmentRepository.DeleteAsync(department);
    }
}

// Program, Semester, Theme Services follow similar patterns
public interface IProgramService
{
    Task<List<ProgramDto>> GetAllAsync();
    Task<ProgramDto?> GetByIdAsync(Guid id);
    Task<ProgramDto> CreateAsync(string name);
    Task<ProgramDto> UpdateAsync(Guid id, string name);
    Task DeleteAsync(Guid id);
}

public class ProgramDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProgramService : IProgramService
{
    private readonly IRepository<Program> _programRepository;
    private readonly IRepository<User> _userRepository;

    public ProgramService(IRepository<Program> programRepository, IRepository<User> userRepository)
    {
        _programRepository = programRepository;
        _userRepository = userRepository;
    }

    public async Task<List<ProgramDto>> GetAllAsync()
    {
        var programs = await _programRepository.GetAllAsync();
        var dtos = new List<ProgramDto>();

        foreach (var prog in programs)
        {
            var userCount = await _userRepository.CountAsync(u => u.ProgramId == prog.Id);
            dtos.Add(new ProgramDto
            {
                Id = prog.Id,
                Name = prog.Name,
                UserCount = userCount,
                CreatedAt = prog.CreatedAt
            });
        }

        return dtos.OrderBy(p => p.Name).ToList();
    }

    public async Task<ProgramDto?> GetByIdAsync(Guid id)
    {
        var prog = await _programRepository.GetByIdAsync(id);
        if (prog == null) return null;

        var userCount = await _userRepository.CountAsync(u => u.ProgramId == prog.Id);
        return new ProgramDto
        {
            Id = prog.Id,
            Name = prog.Name,
            UserCount = userCount,
            CreatedAt = prog.CreatedAt
        };
    }

    public async Task<ProgramDto> CreateAsync(string name)
    {
        var program = new Program
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        await _programRepository.AddAsync(program);
        return (await GetByIdAsync(program.Id))!;
    }

    public async Task<ProgramDto> UpdateAsync(Guid id, string name)
    {
        var program = await _programRepository.GetByIdAsync(id);
        if (program == null)
            throw new KeyNotFoundException("Program not found");

        program.Name = name;
        await _programRepository.UpdateAsync(program);
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var program = await _programRepository.GetByIdAsync(id);
        if (program == null)
            throw new KeyNotFoundException("Program not found");

        await _programRepository.DeleteAsync(program);
    }
}

// Similar services for Semester and Theme
public interface ISemesterService
{
    Task<List<SemesterDto>> GetAllAsync();
    Task<SemesterDto?> GetByIdAsync(Guid id);
    Task<SemesterDto> CreateAsync(string value);
    Task<SemesterDto> UpdateAsync(Guid id, string value);
    Task DeleteAsync(Guid id);
}

public class SemesterDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SemesterService : ISemesterService
{
    private readonly IRepository<Semester> _semesterRepository;
    private readonly IRepository<User> _userRepository;

    public SemesterService(IRepository<Semester> semesterRepository, IRepository<User> userRepository)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
    }

    public async Task<List<SemesterDto>> GetAllAsync()
    {
        var semesters = await _semesterRepository.GetAllAsync();
        var dtos = new List<SemesterDto>();

        foreach (var sem in semesters)
        {
            var userCount = await _userRepository.CountAsync(u => u.SemesterId == sem.Id);
            dtos.Add(new SemesterDto
            {
                Id = sem.Id,
                Value = sem.Value,
                UserCount = userCount,
                CreatedAt = sem.CreatedAt
            });
        }

        return dtos.OrderBy(s => s.Value).ToList();
    }

    public async Task<SemesterDto?> GetByIdAsync(Guid id)
    {
        var sem = await _semesterRepository.GetByIdAsync(id);
        if (sem == null) return null;

        var userCount = await _userRepository.CountAsync(u => u.SemesterId == sem.Id);
        return new SemesterDto
        {
            Id = sem.Id,
            Value = sem.Value,
            UserCount = userCount,
            CreatedAt = sem.CreatedAt
        };
    }

    public async Task<SemesterDto> CreateAsync(string value)
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Value = value,
            CreatedAt = DateTime.UtcNow
        };

        await _semesterRepository.AddAsync(semester);
        return (await GetByIdAsync(semester.Id))!;
    }

    public async Task<SemesterDto> UpdateAsync(Guid id, string value)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
            throw new KeyNotFoundException("Semester not found");

        semester.Value = value;
        await _semesterRepository.UpdateAsync(semester);

        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
            throw new KeyNotFoundException("Semester not found");

        await _semesterRepository.DeleteAsync(semester);
    }
}

public interface IThemeService
{
    Task<List<ThemeDto>> GetAllAsync();
    Task<ThemeDto?> GetByIdAsync(Guid id);
    Task<ThemeDto> CreateAsync(string name);
    Task<ThemeDto> UpdateAsync(Guid id, string name);
    Task DeleteAsync(Guid id);
}

public class ThemeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InputCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ThemeService : IThemeService
{
    private readonly IRepository<Theme> _themeRepository;
    private readonly IRepository<Input> _inputRepository;

    public ThemeService(IRepository<Theme> themeRepository, IRepository<Input> inputRepository)
    {
        _themeRepository = themeRepository;
        _inputRepository = inputRepository;
    }

    public async Task<List<ThemeDto>> GetAllAsync()
    {
        var themes = await _themeRepository.GetAllAsync();
        var dtos = new List<ThemeDto>();

        foreach (var theme in themes)
        {
            var inputCount = await _inputRepository.CountAsync(i => i.ThemeId == theme.Id);
            dtos.Add(new ThemeDto
            {
                Id = theme.Id,
                Name = theme.Name,
                InputCount = inputCount,
                CreatedAt = theme.CreatedAt
            });
        }

        return dtos.OrderBy(t => t.Name).ToList();
    }

    public async Task<ThemeDto?> GetByIdAsync(Guid id)
    {
        var theme = await _themeRepository.GetByIdAsync(id);
        if (theme == null) return null;

        var inputCount = await _inputRepository.CountAsync(i => i.ThemeId == theme.Id);
        return new ThemeDto
        {
            Id = theme.Id,
            Name = theme.Name,
            InputCount = inputCount,
            CreatedAt = theme.CreatedAt
        };
    }

    public async Task<ThemeDto> CreateAsync(string name)
    {
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        await _themeRepository.AddAsync(theme);
        return (await GetByIdAsync(theme.Id))!;
    }

    public async Task<ThemeDto> UpdateAsync(Guid id, string name)
    {
        var theme = await _themeRepository.GetByIdAsync(id);
        if (theme == null)
            throw new KeyNotFoundException("Theme not found");

        theme.Name = name;
        await _themeRepository.UpdateAsync(theme);

        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var theme = await _themeRepository.GetByIdAsync(id);
        if (theme == null)
            throw new KeyNotFoundException("Theme not found");

        await _themeRepository.DeleteAsync(theme);
    }
}
