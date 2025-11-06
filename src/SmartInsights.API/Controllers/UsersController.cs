using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Users;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with optional filtering
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var result = await _userService.GetAllAsync(page, pageSize, role, status);
            return Ok(ApiResponse<PaginatedResult<UserDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PaginatedResult<UserDto>>.ErrorResponse("Failed to retrieve users"));
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("Failed to retrieve user"));
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("email/{email}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        try
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("Failed to retrieve user"));
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, 
                ApiResponse<UserDto>.SuccessResponse(user, "User created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("Failed to create user"));
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userService.UpdateAsync(id, request);
            return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("Failed to update user"));
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete user"));
        }
    }

    /// <summary>
    /// Import users from CSV file
    /// Format: Email, FirstName, LastName, Password, Role, Department, Program, Semester
    /// </summary>
    [HttpPost("import-csv")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ImportFromCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<List<UserDto>>.ErrorResponse("No file uploaded"));
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<List<UserDto>>.ErrorResponse("File must be a CSV"));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var users = await _userService.ImportFromCsvAsync(stream);
            
            return Ok(ApiResponse<List<UserDto>>.SuccessResponse(
                users, 
                $"{users.Count} users imported successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResponse("Failed to import users"));
        }
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var totalCount = await _userService.GetTotalCountAsync();
            var countByRole = await _userService.GetCountByRoleAsync();

            var stats = new
            {
                TotalUsers = totalCount,
                ByRole = countByRole
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve statistics"));
        }
    }
}
