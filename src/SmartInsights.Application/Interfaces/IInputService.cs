using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inputs;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Interfaces;

public interface IInputService
{
    Task<InputDto> CreateAsync(CreateInputRequest request);
    Task<InputDto?> GetByIdAsync(Guid id);
    Task<InputDto> UpdateAsync(Guid id, UpdateInputRequest request);
    Task DeleteAsync(Guid id);
    Task<PaginatedResult<InputDto>> GetFilteredAsync(InputFilterDto filter);
    Task<PaginatedResult<InputDto>> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20);
    Task RequestIdentityRevealAsync(Guid inputId);
    Task RespondToRevealRequestAsync(Guid inputId, bool approved, Guid userId);

    // Replies
    Task<InputReplyDto> CreateReplyAsync(Guid inputId, string message, Guid userId, Role userRole);
    Task<List<InputReplyDto>> GetRepliesAsync(Guid inputId);

    // Statistics
    Task<Dictionary<string, int>> GetCountByTypeAsync();
    Task<Dictionary<string, int>> GetCountBySeverityAsync();
    Task<Dictionary<string, int>> GetCountByStatusAsync();
    Task<Dictionary<string, int>> GetCountBySentimentAsync();
    Task<double> GetAverageQualityScoreAsync();
}
