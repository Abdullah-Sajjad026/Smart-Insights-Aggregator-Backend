using SmartInsights.Application.DTOs.Common;
using SmartInsights.Application.DTOs.Inputs;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Services;

public class InputService : IInputService
{
    private readonly IRepository<Input> _inputRepository;
    private readonly IRepository<InputReply> _inputReplyRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Inquiry> _inquiryRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Theme> _themeRepository;
    private readonly IBackgroundJobService _backgroundJobService;

    public InputService(
        IRepository<Input> inputRepository,
        IRepository<InputReply> inputReplyRepository,
        IRepository<User> userRepository,
        IRepository<Inquiry> inquiryRepository,
        IRepository<Topic> topicRepository,
        IRepository<Theme> themeRepository,
        IBackgroundJobService backgroundJobService)
    {
        _inputRepository = inputRepository;
        _inputReplyRepository = inputReplyRepository;
        _userRepository = userRepository;
        _inquiryRepository = inquiryRepository;
        _topicRepository = topicRepository;
        _themeRepository = themeRepository;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<InputDto> CreateAsync(CreateInputRequest request)
    {
        // Validate user exists
        User? user = null;
        if (request.UserId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(request.UserId.Value);
            if (user == null)
                throw new ArgumentException("User not found");
        }

        // Determine input type
        var inputType = request.InquiryId.HasValue ? InputType.InquiryLinked : InputType.General;

        // If inquiry-linked, validate inquiry exists and is active
        if (inputType == InputType.InquiryLinked && request.InquiryId.HasValue)
        {
            var inquiry = await _inquiryRepository.GetByIdAsync(request.InquiryId.Value);
            if (inquiry == null)
                throw new ArgumentException("Inquiry not found");
            
            if (inquiry.Status != InquiryStatus.Active)
                throw new InvalidOperationException("Inquiry is not active");
        }

        var input = new Input
        {
            Id = Guid.NewGuid(),
            Body = request.Body,
            Type = inputType,
            Status = InputStatus.Pending,
            UserId = request.UserId ?? Guid.Empty, // For anonymous, use empty guid
            InquiryId = request.InquiryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _inputRepository.AddAsync(input);

        // Trigger automatic AI processing in background
        try
        {
            _backgroundJobService.EnqueueInputProcessing(input.Id);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            // The recurring job will pick it up later
            Console.WriteLine($"Failed to enqueue AI processing: {ex.Message}");
        }

        return await GetByIdAsync(input.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created input");
    }

    public async Task<InputDto?> GetByIdAsync(Guid id)
    {
        var input = await _inputRepository.GetByIdAsync(id,
            i => i.User,
            i => i.User.Department!,
            i => i.User.Program!,
            i => i.User.Semester!,
            i => i.Inquiry!,
            i => i.Topic!,
            i => i.Theme!,
            i => i.Replies);

        if (input == null) return null;

        return MapToDto(input);
    }

    public async Task<PaginatedResult<InputDto>> GetFilteredAsync(InputFilterDto filter)
    {
        var inputs = await _inputRepository.GetAllAsync(
            i => i.User,
            i => i.User.Department!,
            i => i.User.Program!,
            i => i.User.Semester!,
            i => i.Inquiry!,
            i => i.Topic!,
            i => i.Theme!,
            i => i.Replies);

        var query = inputs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<InputType>(filter.Type, out var typeEnum))
        {
            query = query.Where(i => i.Type == typeEnum);
        }

        if (filter.InquiryId.HasValue)
        {
            query = query.Where(i => i.InquiryId == filter.InquiryId.Value);
        }

        if (filter.TopicId.HasValue)
        {
            query = query.Where(i => i.TopicId == filter.TopicId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(i => i.User.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Sentiment) && Enum.TryParse<Sentiment>(filter.Sentiment, out var sentimentEnum))
        {
            query = query.Where(i => i.Sentiment == sentimentEnum);
        }

        if (filter.MinQuality.HasValue)
        {
            query = query.Where(i => i.Score >= filter.MinQuality.Value);
        }

        if (filter.Severity.HasValue)
        {
            query = query.Where(i => i.Severity == filter.Severity.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<InputStatus>(filter.Status, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(i => i.Body.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        }

        // Count before sorting and pagination
        var totalCount = query.Count();

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "score" => filter.SortOrder == "asc" ? query.OrderBy(i => i.Score) : query.OrderByDescending(i => i.Score),
            "severity" => filter.SortOrder == "asc" ? query.OrderBy(i => i.Severity) : query.OrderByDescending(i => i.Severity),
            _ => filter.SortOrder == "asc" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt)
        };

        // Apply pagination
        var items = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(MapToDto)
            .ToList();

        return new PaginatedResult<InputDto>
        {
            Items = items,
            CurrentPage = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<List<InputDto>> GetByUserAsync(Guid userId)
    {
        var inputs = await _inputRepository.FindAsync(i => i.UserId == userId,
            i => i.User,
            i => i.User.Department!,
            i => i.User.Program!,
            i => i.User.Semester!,
            i => i.Inquiry!,
            i => i.Topic!,
            i => i.Theme!,
            i => i.Replies);

        return inputs.OrderByDescending(i => i.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task RequestIdentityRevealAsync(Guid inputId)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null)
        {
            throw new KeyNotFoundException("Input not found");
        }

        if (input.RevealRequested)
        {
            throw new InvalidOperationException("Identity reveal already requested");
        }

        input.RevealRequested = true;
        input.UpdatedAt = DateTime.UtcNow;

        await _inputRepository.UpdateAsync(input);

        // TODO: Send notification to student
    }

    public async Task RespondToRevealRequestAsync(Guid inputId, bool approved, Guid userId)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null)
        {
            throw new KeyNotFoundException("Input not found");
        }

        if (input.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only respond to your own reveal requests");
        }

        if (!input.RevealRequested)
        {
            throw new InvalidOperationException("No identity reveal request for this input");
        }

        if (input.RevealApproved.HasValue)
        {
            throw new InvalidOperationException("Already responded to reveal request");
        }

        input.RevealApproved = approved;
        input.UpdatedAt = DateTime.UtcNow;

        await _inputRepository.UpdateAsync(input);
    }

    // Replies
    public async Task<InputReplyDto> CreateReplyAsync(Guid inputId, string message, Guid userId, Role userRole)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null)
        {
            throw new KeyNotFoundException("Input not found");
        }

        // If student, verify ownership
        if (userRole == Role.Student && input.UserId != userId)
        {
            throw new UnauthorizedAccessException("Cannot reply to others' inputs");
        }

        var reply = new InputReply
        {
            Id = Guid.NewGuid(),
            InputId = inputId,
            UserId = userId,
            UserRole = userRole,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        await _inputReplyRepository.AddAsync(reply);

        // Load user for DTO
        var user = await _userRepository.GetByIdAsync(userId);
        
        return new InputReplyDto
        {
            Id = reply.Id,
            Message = reply.Message,
            UserRole = reply.UserRole.ToString(),
            User = new ReplyUserInfo
            {
                Id = user!.Id,
                FirstName = user.FirstName,
                LastName = user.LastName
            },
            CreatedAt = reply.CreatedAt
        };
    }

    public async Task<List<InputReplyDto>> GetRepliesAsync(Guid inputId)
    {
        var replies = await _inputReplyRepository.FindAsync(r => r.InputId == inputId,
            r => r.User);

        return replies.OrderBy(r => r.CreatedAt).Select(r => new InputReplyDto
        {
            Id = r.Id,
            Message = r.Message,
            UserRole = r.UserRole.ToString(),
            User = new ReplyUserInfo
            {
                Id = r.User.Id,
                FirstName = r.User.FirstName,
                LastName = r.User.LastName
            },
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    // Statistics
    public async Task<Dictionary<string, int>> GetCountByTypeAsync()
    {
        var inputs = await _inputRepository.GetAllAsync();
        return inputs.GroupBy(i => i.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetCountBySeverityAsync()
    {
        var inputs = await _inputRepository.GetAllAsync();
        return inputs.Where(i => i.Severity.HasValue)
            .GroupBy(i => i.Severity!.Value switch
            {
                1 => "Low",
                2 => "Medium",
                3 => "High",
                _ => "Unknown"
            })
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetCountByStatusAsync()
    {
        var inputs = await _inputRepository.GetAllAsync();
        return inputs.GroupBy(i => i.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static InputDto MapToDto(Input input)
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
}
