using System.Text.Json;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inquiries;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Services;

public class InquiryService : IInquiryService
{
    private readonly IRepository<Inquiry> _inquiryRepository;
    private readonly IRepository<InquiryDepartment> _inquiryDepartmentRepository;
    private readonly IRepository<InquiryProgram> _inquiryProgramRepository;
    private readonly IRepository<InquirySemester> _inquirySemesterRepository;
    private readonly IRepository<InquiryFaculty> _inquiryFacultyRepository;
    private readonly IRepository<Faculty> _facultyRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<Program> _programRepository;
    private readonly IRepository<Semester> _semesterRepository;
    private readonly IRepository<Input> _inputRepository;
    private readonly IRepository<User> _userRepository;

    public InquiryService(
        IRepository<Inquiry> inquiryRepository,
        IRepository<InquiryDepartment> inquiryDepartmentRepository,
        IRepository<InquiryProgram> inquiryProgramRepository,
        IRepository<InquirySemester> inquirySemesterRepository,
        IRepository<InquiryFaculty> inquiryFacultyRepository,
        IRepository<Department> departmentRepository,
        IRepository<Program> programRepository,
        IRepository<Semester> semesterRepository,
        IRepository<Faculty> facultyRepository,
        IRepository<Input> inputRepository,
        IRepository<User> userRepository)
    {
        _inquiryRepository = inquiryRepository;
        _inquiryDepartmentRepository = inquiryDepartmentRepository;
        _inquiryProgramRepository = inquiryProgramRepository;
        _inquirySemesterRepository = inquirySemesterRepository;
        _inquiryFacultyRepository = inquiryFacultyRepository;
        _departmentRepository = departmentRepository;
        _programRepository = programRepository;
        _semesterRepository = semesterRepository;
        _facultyRepository = facultyRepository;
        _inputRepository = inputRepository;
        _userRepository = userRepository;
    }

    public async Task<InquiryDto> CreateAsync(CreateInquiryRequest request, Guid createdById)
    {
        // Validate status
        if (!Enum.TryParse<InquiryStatus>(request.Status, out var status))
        {
            throw new ArgumentException("Invalid status");
        }

        var inquiry = new Inquiry
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            Status = status,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };

        await _inquiryRepository.AddAsync(inquiry);

        // Add department relationships
        foreach (var deptId in request.DepartmentIds)
        {
            await _inquiryDepartmentRepository.AddAsync(new InquiryDepartment
            {
                InquiryId = inquiry.Id,
                DepartmentId = deptId
            });
        }

        // Add program relationships
        foreach (var progId in request.ProgramIds)
        {
            await _inquiryProgramRepository.AddAsync(new InquiryProgram
            {
                InquiryId = inquiry.Id,
                ProgramId = progId
            });
        }

        // Add semester relationships
        foreach (var semId in request.SemesterIds)
        {
            await _inquirySemesterRepository.AddAsync(new InquirySemester
            {
                InquiryId = inquiry.Id,
                SemesterId = semId
            });
        }

        // Add faculty relationships
        foreach (var facId in request.FacultyIds)
        {
            await _inquiryFacultyRepository.AddAsync(new InquiryFaculty
            {
                InquiryId = inquiry.Id,
                FacultyId = facId
            });
        }

        return await GetByIdAsync(inquiry.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created inquiry");
    }

    public async Task<InquiryDto?> GetByIdAsync(Guid id)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(id,
            i => i.CreatedBy,
            i => i.InquiryDepartments,
            i => i.InquiryPrograms,
            i => i.InquirySemesters,
            i => i.InquiryFaculties);

        if (inquiry == null) return null;

        // Load related entities
        var departments = new List<string>();
        foreach (var id_item in inquiry.InquiryDepartments)
        {
            var dept = await _departmentRepository.GetByIdAsync(id_item.DepartmentId);
            if (dept != null) departments.Add(dept.Name);
        }

        var programs = new List<string>();
        foreach (var ip in inquiry.InquiryPrograms)
        {
            var prog = await _programRepository.GetByIdAsync(ip.ProgramId);
            if (prog != null) programs.Add(prog.Name);
        }

        var semesters = new List<string>();
        foreach (var isem in inquiry.InquirySemesters)
        {
            var sem = await _semesterRepository.GetByIdAsync(isem.SemesterId);
            if (sem != null) semesters.Add(sem.Value);
        }

        var faculties = new List<string>();
        foreach (var ifac in inquiry.InquiryFaculties)
        {
            var fac = await _facultyRepository.GetByIdAsync(ifac.FacultyId);
            if (fac != null) faculties.Add(fac.Name);
        }

        // Get stats
        var stats = await GetStatsAsync(id);

        // Parse summary if exists
        ExecutiveSummaryDto? summaryDto = null;
        if (!string.IsNullOrEmpty(inquiry.Summary))
        {
            try
            {
                var summary = JsonSerializer.Deserialize<ExecutiveSummary>(inquiry.Summary);
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
                        GeneratedAt = inquiry.SummaryGeneratedAt ?? DateTime.UtcNow
                    };
                }
            }
            catch { /* Ignore parsing errors */ }
        }

        return new InquiryDto
        {
            Id = inquiry.Id,
            Title = inquiry.Title,
            Body = inquiry.Body,
            Status = inquiry.Status.ToString(),
            CreatedBy = new InquiryCreatorInfo
            {
                Id = inquiry.CreatedBy.Id,
                FirstName = inquiry.CreatedBy.FirstName,
                LastName = inquiry.CreatedBy.LastName,
                Email = inquiry.CreatedBy.Email
            },
            TargetDepartments = departments,
            TargetPrograms = programs,
            TargetSemesters = semesters,
            TargetFaculties = faculties,
            Stats = stats,
            AiSummary = summaryDto,
            CreatedAt = inquiry.CreatedAt,
            SentAt = inquiry.SentAt,
            ClosedAt = inquiry.ClosedAt
        };
    }

    public async Task<PaginatedResult<InquiryDto>> GetAllAsync(int page = 1, int pageSize = 20, string? status = null, Guid? departmentId = null, Guid? createdById = null)
    {
        var inquiries = await _inquiryRepository.GetAllAsync(
            i => i.CreatedBy,
            i => i.InquiryDepartments,
            i => i.InquiryPrograms,
            i => i.InquirySemesters);

        var query = inquiries.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InquiryStatus>(status, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(i => i.InquiryDepartments.Any(id => id.DepartmentId == departmentId.Value));
        }

        if (createdById.HasValue)
        {
            query = query.Where(i => i.CreatedById == createdById.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<InquiryDto>();
        foreach (var inquiry in items)
        {
            var dto = await GetByIdAsync(inquiry.Id);
            if (dto != null) dtos.Add(dto);
        }

        return new PaginatedResult<InquiryDto>
        {
            Items = dtos,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<InquiryDto> UpdateAsync(Guid id, UpdateInquiryRequest request)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(id);
        if (inquiry == null)
        {
            throw new KeyNotFoundException("Inquiry not found");
        }

        // Only Draft inquiries can be updated
        if (inquiry.Status != InquiryStatus.Draft)
        {
            throw new InvalidOperationException("Only draft inquiries can be updated");
        }

        if (!string.IsNullOrEmpty(request.Title))
        {
            inquiry.Title = request.Title;
        }

        if (!string.IsNullOrEmpty(request.Body))
        {
            inquiry.Body = request.Body;
        }

        // Update department relationships if provided
        if (request.DepartmentIds != null)
        {
            var existingDepts = await _inquiryDepartmentRepository.FindAsync(id => id.InquiryId == inquiry.Id);
            await _inquiryDepartmentRepository.DeleteRangeAsync(existingDepts);

            foreach (var deptId in request.DepartmentIds)
            {
                await _inquiryDepartmentRepository.AddAsync(new InquiryDepartment
                {
                    InquiryId = inquiry.Id,
                    DepartmentId = deptId
                });
            }
        }

        // Update program relationships if provided
        if (request.ProgramIds != null)
        {
            var existingProgs = await _inquiryProgramRepository.FindAsync(ip => ip.InquiryId == inquiry.Id);
            await _inquiryProgramRepository.DeleteRangeAsync(existingProgs);

            foreach (var progId in request.ProgramIds)
            {
                await _inquiryProgramRepository.AddAsync(new InquiryProgram
                {
                    InquiryId = inquiry.Id,
                    ProgramId = progId
                });
            }
        }

        // Update semester relationships if provided
        if (request.SemesterIds != null)
        {
            var existingSems = await _inquirySemesterRepository.FindAsync(isem => isem.InquiryId == inquiry.Id);
            await _inquirySemesterRepository.DeleteRangeAsync(existingSems);

            foreach (var semId in request.SemesterIds)
            {
                await _inquirySemesterRepository.AddAsync(new InquirySemester
                {
                    InquiryId = inquiry.Id,
                    SemesterId = semId
                });
            }
        }

        await _inquiryRepository.UpdateAsync(inquiry);

        return await GetByIdAsync(inquiry.Id)
            ?? throw new InvalidOperationException("Failed to retrieve updated inquiry");
    }

    public async Task<InquiryDto> SendAsync(Guid id)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(id);
        if (inquiry == null)
        {
            throw new KeyNotFoundException("Inquiry not found");
        }

        if (inquiry.Status != InquiryStatus.Draft)
        {
            throw new InvalidOperationException("Only draft inquiries can be sent");
        }

        inquiry.Status = InquiryStatus.Active;
        inquiry.SentAt = DateTime.UtcNow;

        await _inquiryRepository.UpdateAsync(inquiry);

        return await GetByIdAsync(inquiry.Id)
            ?? throw new InvalidOperationException("Failed to retrieve sent inquiry");
    }

    public async Task<InquiryDto> CloseAsync(Guid id)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(id);
        if (inquiry == null)
        {
            throw new KeyNotFoundException("Inquiry not found");
        }

        if (inquiry.Status != InquiryStatus.Active)
        {
            throw new InvalidOperationException("Only active inquiries can be closed");
        }

        inquiry.Status = InquiryStatus.Closed;
        inquiry.ClosedAt = DateTime.UtcNow;

        await _inquiryRepository.UpdateAsync(inquiry);

        return await GetByIdAsync(inquiry.Id)
            ?? throw new InvalidOperationException("Failed to retrieve closed inquiry");
    }

    public async Task DeleteAsync(Guid id)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(id);
        if (inquiry == null)
        {
            throw new KeyNotFoundException("Inquiry not found");
        }

        // Only draft inquiries can be deleted
        if (inquiry.Status != InquiryStatus.Draft)
        {
            throw new InvalidOperationException("Only draft inquiries can be deleted");
        }

        await _inquiryRepository.DeleteAsync(inquiry);
    }

    public async Task<InquiryStats> GetStatsAsync(Guid inquiryId)
    {
        var inputs = await _inputRepository.FindAsync(i => i.InquiryId == inquiryId);

        var totalResponses = inputs.Count;
        var averageQuality = totalResponses > 0 && inputs.Any(i => i.Score.HasValue)
            ? inputs.Where(i => i.Score.HasValue).Average(i => i.Score!.Value)
            : 0.0;

        var sentimentBreakdown = inputs
            .Where(i => i.Sentiment.HasValue)
            .GroupBy(i => i.Sentiment!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var severityBreakdown = inputs
            .Where(i => i.Severity.HasValue)
            .GroupBy(i => i.Severity!.Value switch
            {
                1 => "Low",
                2 => "Medium",
                3 => "High",
                _ => "Unknown"
            })
            .ToDictionary(g => g.Key, g => g.Count());

        return new InquiryStats
        {
            TotalResponses = totalResponses,
            AverageQuality = averageQuality,
            SentimentBreakdown = sentimentBreakdown,
            SeverityBreakdown = severityBreakdown
        };
    }

    public async Task<List<InquiryDto>> GetByCreatorAsync(Guid creatorId)
    {
        var inquiries = await _inquiryRepository.FindAsync(i => i.CreatedById == creatorId);
        var dtos = new List<InquiryDto>();

        foreach (var inquiry in inquiries.OrderByDescending(i => i.CreatedAt))
        {
            var dto = await GetByIdAsync(inquiry.Id);
            if (dto != null) dtos.Add(dto);
        }

        return dtos;
    }
    public async Task<List<InquiryDto>> GetForStudentAsync(Guid studentId)
    {
        var user = await _userRepository.GetByIdAsync(studentId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // Get all active inquiries
        var inquiries = await _inquiryRepository.GetAllAsync(
            i => i.CreatedBy,
            i => i.InquiryDepartments,
            i => i.InquiryPrograms,
            i => i.InquirySemesters);

        var activeInquiries = inquiries.Where(i => i.Status == InquiryStatus.Active).ToList();
        var relevantInquiries = new List<Inquiry>();

        foreach (var inquiry in activeInquiries)
        {
            bool isRelevant = true;

            // Check department restriction
            if (inquiry.InquiryDepartments.Any())
            {
                if (!user.DepartmentId.HasValue || !inquiry.InquiryDepartments.Any(id => id.DepartmentId == user.DepartmentId.Value))
                {
                    isRelevant = false;
                }
            }

            // Check program restriction
            if (isRelevant && inquiry.InquiryPrograms.Any())
            {
                if (!user.ProgramId.HasValue || !inquiry.InquiryPrograms.Any(ip => ip.ProgramId == user.ProgramId.Value))
                {
                    isRelevant = false;
                }
            }

            // Check semester restriction
            if (isRelevant && inquiry.InquirySemesters.Any())
            {
                if (!user.SemesterId.HasValue || !inquiry.InquirySemesters.Any(issem => issem.SemesterId == user.SemesterId.Value))
                {
                    isRelevant = false;
                }
            }

            if (isRelevant)
            {
                relevantInquiries.Add(inquiry);
            }
        }

        var dtos = new List<InquiryDto>();
        foreach (var inquiry in relevantInquiries.OrderByDescending(i => i.CreatedAt))
        {
            var dto = await GetByIdAsync(inquiry.Id);
            if (dto != null) dtos.Add(dto);
        }

        return dtos;
    }
}
