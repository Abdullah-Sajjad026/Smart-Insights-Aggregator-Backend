using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Users;

namespace SmartInsights.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<PaginatedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20, string? role = null, string? status = null);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
    Task<List<UserDto>> ImportFromCsvAsync(Stream csvStream);
    Task<int> GetTotalCountAsync();
    Task<Dictionary<string, int>> GetCountByRoleAsync();
}
