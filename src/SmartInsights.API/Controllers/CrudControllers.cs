using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.Interfaces;
using SmartInsights.Application.Services;
using SmartInsights.Domain.Entities;

namespace SmartInsights.API.Controllers;

// Topics Controller
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;

    public TopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _topicService.GetAllAsync(page, pageSize);
            return Ok(ApiResponse<PaginatedResult<TopicDto>>.SuccessResponse(result));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<TopicDto>>.ErrorResponse("Failed to retrieve topics"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var topic = await _topicService.GetByIdAsync(id);
            if (topic == null)
                return NotFound(ApiResponse<TopicDto>.ErrorResponse("Topic not found"));

            return Ok(ApiResponse<TopicDto>.SuccessResponse(topic));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<TopicDto>.ErrorResponse("Failed to retrieve topic"));
        }
    }

    [HttpGet("by-department/{departmentId}")]
    public async Task<IActionResult> GetByDepartment(Guid departmentId)
    {
        try
        {
            var topics = await _topicService.GetByDepartmentAsync(departmentId);
            return Ok(ApiResponse<List<TopicDto>>.SuccessResponse(topics));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<List<TopicDto>>.ErrorResponse("Failed to retrieve topics"));
        }
    }

    /// <summary>
    /// Get topic statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStats([FromServices] IRepository<Topic> topicRepository, [FromServices] IRepository<Input> inputRepository)
    {
        try
        {
            var totalTopics = await topicRepository.CountAsync();
            var topicsWithSummaries = await topicRepository.CountAsync(t => !string.IsNullOrEmpty(t.Summary));
            var totalInputsInTopics = await inputRepository.CountAsync(i => i.TopicId != null);

            var stats = new
            {
                TotalTopics = totalTopics,
                TopicsWithAISummaries = topicsWithSummaries,
                TotalInputsInTopics = totalInputsInTopics,
                AverageInputsPerTopic = totalTopics > 0 ? (double)totalInputsInTopics / totalTopics : 0
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve statistics"));
        }
    }
}

// Departments Controller (enhanced)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var departments = await _departmentService.GetAllAsync();
            return Ok(ApiResponse<List<DepartmentDto>>.SuccessResponse(departments));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<List<DepartmentDto>>.ErrorResponse("Failed to retrieve departments"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var department = await _departmentService.GetByIdAsync(id);
            if (department == null)
                return NotFound(ApiResponse<DepartmentDto>.ErrorResponse("Department not found"));

            return Ok(ApiResponse<DepartmentDto>.SuccessResponse(department));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse("Failed to retrieve department"));
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        try
        {
            var department = await _departmentService.CreateAsync(request.Name, request.Description);
            return CreatedAtAction(nameof(GetById), new { id = department.Id },
                ApiResponse<DepartmentDto>.SuccessResponse(department, "Department created"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<DepartmentDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse("Failed to create department"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        try
        {
            var department = await _departmentService.UpdateAsync(id, request.Name, request.Description);
            return Ok(ApiResponse<DepartmentDto>.SuccessResponse(department, "Department updated"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DepartmentDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse("Failed to update department"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _departmentService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Department deleted"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete department"));
        }
    }
}

// Programs Controller
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgramsController : ControllerBase
{
    private readonly IProgramService _programService;

    public ProgramsController(IProgramService programService)
    {
        _programService = programService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var programs = await _programService.GetAllAsync();
            return Ok(ApiResponse<List<ProgramDto>>.SuccessResponse(programs));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<List<ProgramDto>>.ErrorResponse("Failed to retrieve programs"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var program = await _programService.GetByIdAsync(id);
            if (program == null)
                return NotFound(ApiResponse<ProgramDto>.ErrorResponse("Program not found"));

            return Ok(ApiResponse<ProgramDto>.SuccessResponse(program));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ProgramDto>.ErrorResponse("Failed to retrieve program"));
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest request)
    {
        try
        {
            var program = await _programService.CreateAsync(request.Name);
            return CreatedAtAction(nameof(GetById), new { id = program.Id },
                ApiResponse<ProgramDto>.SuccessResponse(program, "Program created"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ProgramDto>.ErrorResponse("Failed to create program"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProgramRequest request)
    {
        try
        {
            var program = await _programService.UpdateAsync(id, request.Name);
            return Ok(ApiResponse<ProgramDto>.SuccessResponse(program, "Program updated"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProgramDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ProgramDto>.ErrorResponse("Failed to update program"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _programService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Program deleted"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete program"));
        }
    }
}

// Semesters Controller
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var semesters = await _semesterService.GetAllAsync();
            return Ok(ApiResponse<List<SemesterDto>>.SuccessResponse(semesters));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<List<SemesterDto>>.ErrorResponse("Failed to retrieve semesters"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var semester = await _semesterService.GetByIdAsync(id);
            if (semester == null)
                return NotFound(ApiResponse<SemesterDto>.ErrorResponse("Semester not found"));

            return Ok(ApiResponse<SemesterDto>.SuccessResponse(semester));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<SemesterDto>.ErrorResponse("Failed to retrieve semester"));
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequest request)
    {
        try
        {
            var semester = await _semesterService.CreateAsync(request.Value);
            return Ok(ApiResponse<SemesterDto>.SuccessResponse(semester, "Semester created"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<SemesterDto>.ErrorResponse("Failed to create semester"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSemesterRequest request)
    {
        try
        {
            var semester = await _semesterService.UpdateAsync(id, request.Value);
            return Ok(ApiResponse<SemesterDto>.SuccessResponse(semester, "Semester updated"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SemesterDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<SemesterDto>.ErrorResponse("Failed to update semester"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _semesterService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Semester deleted"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete semester"));
        }
    }
}

// Themes Controller
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThemesController : ControllerBase
{
    private readonly IThemeService _themeService;

    public ThemesController(IThemeService themeService)
    {
        _themeService = themeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var themes = await _themeService.GetAllAsync();
            return Ok(ApiResponse<List<ThemeDto>>.SuccessResponse(themes));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<List<ThemeDto>>.ErrorResponse("Failed to retrieve themes"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var theme = await _themeService.GetByIdAsync(id);
            if (theme == null)
                return NotFound(ApiResponse<ThemeDto>.ErrorResponse("Theme not found"));

            return Ok(ApiResponse<ThemeDto>.SuccessResponse(theme));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ThemeDto>.ErrorResponse("Failed to retrieve theme"));
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateThemeRequest request)
    {
        try
        {
            var theme = await _themeService.CreateAsync(request.Name);
            return Ok(ApiResponse<ThemeDto>.SuccessResponse(theme, "Theme created"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ThemeDto>.ErrorResponse("Failed to create theme"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateThemeRequest request)
    {
        try
        {
            var theme = await _themeService.UpdateAsync(id, request.Name);
            return Ok(ApiResponse<ThemeDto>.SuccessResponse(theme, "Theme updated"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ThemeDto>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<ThemeDto>.ErrorResponse("Failed to update theme"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _themeService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Theme deleted"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete theme"));
        }
    }
}

// Request DTOs
public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreateProgramRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateProgramRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateSemesterRequest
{
    public string Value { get; set; } = string.Empty;
}

public class UpdateSemesterRequest
{
    public string Value { get; set; } = string.Empty;
}

public class CreateThemeRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateThemeRequest
{
    public string Name { get; set; } = string.Empty;
}
