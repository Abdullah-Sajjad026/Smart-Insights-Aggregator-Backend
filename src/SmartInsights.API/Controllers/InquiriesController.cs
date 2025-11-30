using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inquiries;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;

namespace SmartInsights.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InquiriesController : ControllerBase
{
    private readonly IInquiryService _inquiryService;

    public InquiriesController(IInquiryService inquiryService)
    {
        _inquiryService = inquiryService;
    }

    /// <summary>
    /// Get all inquiries with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? createdById = null)
    {
        try
        {
            var result = await _inquiryService.GetAllAsync(page, pageSize, status, departmentId, createdById);
            return Ok(ApiResponse<PaginatedResult<InquiryDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<InquiryDto>>.ErrorResponse("Failed to retrieve inquiries"));
        }
    }

    /// <summary>
    /// Get inquiry by ID with full details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var inquiry = await _inquiryService.GetByIdAsync(id);
            if (inquiry == null)
            {
                return NotFound(ApiResponse<InquiryDto>.ErrorResponse("Inquiry not found"));
            }

            return Ok(ApiResponse<InquiryDto>.SuccessResponse(inquiry));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryDto>.ErrorResponse("Failed to retrieve inquiry"));
        }
    }

    /// <summary>
    /// Get inquiries created by current user
    /// </summary>
    [HttpGet("my-inquiries")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetMyInquiries()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var inquiries = await _inquiryService.GetByCreatorAsync(userId);
            return Ok(ApiResponse<List<InquiryDto>>.SuccessResponse(inquiries));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<InquiryDto>>.ErrorResponse("Failed to retrieve inquiries"));
        }
    }

    /// <summary>
    /// Get inquiries targeted to the current student
    /// </summary>
    [HttpGet("for-student")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetForStudent()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var inquiries = await _inquiryService.GetForStudentAsync(userId);
            return Ok(ApiResponse<List<InquiryDto>>.SuccessResponse(inquiries));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<InquiryDto>>.ErrorResponse("Failed to retrieve inquiries"));
        }
    }

    /// <summary>
    /// Create a new inquiry (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateInquiryRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var inquiry = await _inquiryService.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetById), new { id = inquiry.Id },
                ApiResponse<InquiryDto>.SuccessResponse(inquiry, "Inquiry created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryDto>.ErrorResponse("Failed to create inquiry"));
        }
    }

    /// <summary>
    /// Update an inquiry (Admin only, Draft only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInquiryRequest request)
    {
        try
        {
            var inquiry = await _inquiryService.UpdateAsync(id, request);
            return Ok(ApiResponse<InquiryDto>.SuccessResponse(inquiry, "Inquiry updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryDto>.ErrorResponse("Failed to update inquiry"));
        }
    }

    /// <summary>
    /// Send inquiry (changes status from Draft to Active)
    /// </summary>
    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Send(Guid id)
    {
        try
        {
            var inquiry = await _inquiryService.SendAsync(id);
            return Ok(ApiResponse<InquiryDto>.SuccessResponse(inquiry, "Inquiry sent successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryDto>.ErrorResponse("Failed to send inquiry"));
        }
    }

    /// <summary>
    /// Close inquiry (changes status from Active to Closed)
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            var inquiry = await _inquiryService.CloseAsync(id);
            return Ok(ApiResponse<InquiryDto>.SuccessResponse(inquiry, "Inquiry closed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InquiryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryDto>.ErrorResponse("Failed to close inquiry"));
        }
    }

    /// <summary>
    /// Delete an inquiry (Admin only, Draft only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _inquiryService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Inquiry deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete inquiry"));
        }
    }

    /// <summary>
    /// Get general inquiry statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetGeneralStats([FromServices] IRepository<Inquiry> inquiryRepository)
    {
        try
        {
            var totalInquiries = await inquiryRepository.CountAsync();
            var draftInquiries = await inquiryRepository.CountAsync(i => i.Status == Domain.Enums.InquiryStatus.Draft);
            var activeInquiries = await inquiryRepository.CountAsync(i => i.Status == Domain.Enums.InquiryStatus.Active);
            var closedInquiries = await inquiryRepository.CountAsync(i => i.Status == Domain.Enums.InquiryStatus.Closed);

            var stats = new
            {
                Total = totalInquiries,
                ByStatus = new
                {
                    Draft = draftInquiries,
                    Active = activeInquiries,
                    Closed = closedInquiries
                }
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve statistics"));
        }
    }

    /// <summary>
    /// Generate summary for an inquiry (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/generate-summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GenerateSummary(Guid id, [FromServices] IBackgroundJobService backgroundJobService)
    {
        try
        {
            // Verify inquiry exists
            var inquiry = await _inquiryService.GetByIdAsync(id);
            if (inquiry == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Inquiry not found"));
            }

            // Enqueue background job
            backgroundJobService.EnqueueInquirySummaryGeneration(id);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Summary generation started"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to start summary generation"));
        }
    }

    /// <summary>
    /// Get inquiry statistics for a specific inquiry
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetInquiryStats(Guid id)
    {
        try
        {
            var stats = await _inquiryService.GetStatsAsync(id);
            return Ok(ApiResponse<InquiryStats>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InquiryStats>.ErrorResponse("Failed to retrieve statistics"));
        }
    }
}
