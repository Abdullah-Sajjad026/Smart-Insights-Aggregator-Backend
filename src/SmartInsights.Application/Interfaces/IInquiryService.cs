using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inquiries;

namespace SmartInsights.Application.Interfaces;

public interface IInquiryService
{
    Task<InquiryDto> CreateAsync(CreateInquiryRequest request, Guid createdById);
    Task<InquiryDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<InquiryDto>> GetAllAsync(int page = 1, int pageSize = 20, string? status = null, Guid? departmentId = null, Guid? createdById = null);
    Task<InquiryDto> UpdateAsync(Guid id, UpdateInquiryRequest request);
    Task<InquiryDto> SendAsync(Guid id); // Changes status to Active
    Task<InquiryDto> CloseAsync(Guid id); // Changes status to Closed
    Task DeleteAsync(Guid id);
    Task<InquiryStats> GetStatsAsync(Guid inquiryId);
    Task<List<InquiryDto>> GetByCreatorAsync(Guid creatorId);
    Task<List<InquiryDto>> GetForStudentAsync(Guid studentId);
}
