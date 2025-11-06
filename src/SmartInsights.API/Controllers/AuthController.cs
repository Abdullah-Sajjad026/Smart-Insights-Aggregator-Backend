using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.DTOs.Auth;
using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("An error occurred during login"));
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        try
        {
            var isValid = await _authService.ValidateTokenAsync(token);
            return Ok(ApiResponse<bool>.SuccessResponse(isValid));
        }
        catch (Exception)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(false));
        }
    }
}
