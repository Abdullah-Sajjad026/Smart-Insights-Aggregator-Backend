using System.Text.Json;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inquiries;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Application.DTOs.Faculties;
using SmartInsights.Application.DTOs.Inputs;

namespace SmartInsights.Application.Services;

// Topic Service
public interface ITopicService
{
    Task<TopicDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<TopicDto>> GetAllAsync(int page = 1, int pageSize = 20, bool includeArchived = false);
    Task<List<TopicDto>> GetByDepartmentAsync(Guid departmentId, bool includeArchived = false);
    Task ArchiveAsync(Guid id);
    Task UnarchiveAsync(Guid id);
}

public class TopicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsArchived { get; set; }
    public int InputCount { get; set; }
    public ExecutiveSummaryDto? AiSummary { get; set; }
    public List<InputDto> Inputs { get; set; } = new();
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

        var inputs = await _inputRepository.FindAsync(i => i.TopicId == id,
            i => i.User,
            i => i.User.Department!,
            i => i.User.Program!,
            i => i.User.Semester!,
            i => i.Inquiry!,
            i => i.Topic!,
            i => i.Theme!);

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
            IsArchived = topic.IsArchived,
            InputCount = inputCount,
            AiSummary = summaryDto,
            Inputs = inputs.OrderByDescending(i => i.CreatedAt).Select(MapInputToDto).ToList(),
            CreatedAt = topic.CreatedAt
        };
    }

    public async Task<PaginatedResult<TopicDto>> GetAllAsync(int page = 1, int pageSize = 20, bool includeArchived = false)
    {
        var topics = await _topicRepository.GetAllAsync(t => t.Department!);
        if (!includeArchived)
        {
            topics = topics.Where(t => !t.IsArchived).ToList();
        }

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

    public async Task<List<TopicDto>> GetByDepartmentAsync(Guid departmentId, bool includeArchived = false)
    {
        var topics = await _topicRepository.FindAsync(t => t.DepartmentId == departmentId && (includeArchived || !t.IsArchived), t => t.Department!);

        var dtos = new List<TopicDto>();
        foreach (var topic in topics)
        {
            var dto = await GetByIdAsync(topic.Id);
            if (dto != null) dtos.Add(dto);
        }

        return dtos.OrderByDescending(t => t.InputCount).ToList();
    }

    private InputDto MapInputToDto(Input input)
    {
        return new InputDto
        {
            Id = input.Id,
            Body = input.Body,
            Type = input.Type.ToString(),
            Status = input.Status.ToString(),
            Sentiment = input.Sentiment?.ToString(),
            Tone = input.Tone?.ToString(),
            Metrics = input.Score.HasValue ? new QualityMetrics
            {
                Urgency = input.UrgencyPct ?? 0,
                Importance = input.ImportancePct ?? 0,
                Clarity = input.ClarityPct ?? 0,
                Quality = input.QualityPct ?? 0,
                Helpfulness = input.HelpfulnessPct ?? 0,
                Score = input.Score.Value,
                Severity = input.Severity ?? 0
            } : null,
            User = new InputUserInfo
            {
                Department = input.User?.Department?.Name,
                Program = input.User?.Program?.Name,
                Semester = input.User?.Semester?.Value,
                IsAnonymous = input.IsAnonymous,
                FirstName = input.RevealApproved == true ? input.User?.FirstName : null,
                LastName = input.RevealApproved == true ? input.User?.LastName : null,
                Email = input.RevealApproved == true ? input.User?.Email : null
            },
            Inquiry = input.Inquiry != null ? new InquiryBasicInfo
            {
                Id = input.Inquiry.Id,
                Body = input.Inquiry.Body
            } : null,
            Topic = input.Topic != null ? new TopicBasicInfo
            {
                Id = input.Topic.Id,
                Name = input.Topic.Name
            } : null,
            Theme = input.Theme != null ? new ThemeBasicInfo
            {
                Id = input.Theme.Id,
                Name = input.Theme.Name
            } : null,
            ReplyCount = input.Replies?.Count ?? 0,
            RevealRequested = input.RevealRequested,
            RevealApproved = input.RevealApproved,
            CreatedAt = input.CreatedAt
        };
    }

    public async Task ArchiveAsync(Guid id)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null) throw new KeyNotFoundException("Topic not found");

        topic.IsArchived = true;
        topic.UpdatedAt = DateTime.UtcNow;
        await _topicRepository.UpdateAsync(topic);
    }

    public async Task UnarchiveAsync(Guid id)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null) throw new KeyNotFoundException("Topic not found");

        topic.IsArchived = false;
        topic.UpdatedAt = DateTime.UtcNow;
        await _topicRepository.UpdateAsync(topic);
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

        var userCount = await _userRepository.CountAsync(u => u.DepartmentId == id);
        if (userCount > 0)
            throw new InvalidOperationException($"Cannot delete department because it has {userCount} users associated with it.");

        var topicCount = await _topicRepository.CountAsync(t => t.DepartmentId == id);
        if (topicCount > 0)
            throw new InvalidOperationException($"Cannot delete department because it has {topicCount} topics associated with it.");

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

        var userCount = await _userRepository.CountAsync(u => u.ProgramId == id);
        if (userCount > 0)
            throw new InvalidOperationException($"Cannot delete program because it has {userCount} users associated with it.");

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

        var userCount = await _userRepository.CountAsync(u => u.SemesterId == id);
        if (userCount > 0)
            throw new InvalidOperationException($"Cannot delete semester because it has {userCount} users associated with it.");

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

// Faculty Service
public interface IFacultyService
{
    Task<List<FacultyDto>> GetAllAsync();
    Task<FacultyDto?> GetByIdAsync(Guid id);
    Task<FacultyDto> CreateAsync(string name, string? description);
    Task<FacultyDto> UpdateAsync(Guid id, string name, string? description);
    Task DeleteAsync(Guid id);
}

public class FacultyService : IFacultyService
{
    private readonly IRepository<Faculty> _facultyRepository;

    public FacultyService(IRepository<Faculty> facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<List<FacultyDto>> GetAllAsync()
    {
        var faculties = await _facultyRepository.GetAllAsync();
        return faculties.Select(f => new FacultyDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            CreatedAt = f.CreatedAt
        }).OrderBy(f => f.Name).ToList();
    }

    public async Task<FacultyDto?> GetByIdAsync(Guid id)
    {
        var faculty = await _facultyRepository.GetByIdAsync(id);
        if (faculty == null) return null;

        return new FacultyDto
        {
            Id = faculty.Id,
            Name = faculty.Name,
            Description = faculty.Description,
            CreatedAt = faculty.CreatedAt
        };
    }

    public async Task<FacultyDto> CreateAsync(string name, string? description)
    {
        var faculty = new Faculty
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await _facultyRepository.AddAsync(faculty);
        return (await GetByIdAsync(faculty.Id))!;
    }

    public async Task<FacultyDto> UpdateAsync(Guid id, string name, string? description)
    {
        var faculty = await _facultyRepository.GetByIdAsync(id);
        if (faculty == null)
            throw new KeyNotFoundException("Faculty not found");

        faculty.Name = name;
        faculty.Description = description;
        faculty.UpdatedAt = DateTime.UtcNow;

        await _facultyRepository.UpdateAsync(faculty);
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var faculty = await _facultyRepository.GetByIdAsync(id);
        if (faculty == null)
            throw new KeyNotFoundException("Faculty not found");

        await _facultyRepository.DeleteAsync(faculty);
    }
}
