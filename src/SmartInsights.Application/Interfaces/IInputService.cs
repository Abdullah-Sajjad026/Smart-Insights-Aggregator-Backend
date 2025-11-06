using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inputs;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Interfaces;

public interface IInputService
{
    Task<InputDto> CreateAsync(CreateInputRequest request);
    Task<InputDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<InputDto>> GetFilteredAsync(InputFilterDto filter);
    Task<List<InputDto>> GetByUserAsync(Guid userId);
    Task RequestIdentityRevealAsync(Guid inputId);
    Task RespondToRevealRequestAsync(Guid inputId, bool approved, Guid userId);
    
    // Replies
    Task<InputReplyDto> CreateReplyAsync(Guid inputId, string message, Guid userId, Role userRole);
    Task<List<InputReplyDto>> GetRepliesAsync(Guid inputId);
    
    // Statistics
    Task<Dictionary<string, int>> GetCountByTypeAsync();
    Task<Dictionary<string, int>> GetCountBySeverityAsync();
    Task<Dictionary<string, int>> GetCountByStatusAsync();
}
