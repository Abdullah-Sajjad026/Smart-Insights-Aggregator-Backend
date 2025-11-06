# Backend Plan - Required Modifications Summary

**Date:** November 5, 2024
**Based on:** DOTNET_BACKEND_PLAN.md comments

---

## Overview

This document consolidates all the "Modify" comments found in the backend plan and provides the complete implementation details for each modification.

---

## Modification 1: Inquiry Targeting - Add Programs and Semesters

**Location:** Line 421
**Comment:** "An Inquiry will also have InquiryPrograms, and InquirySemesters as well so need to update it and anything related like dto, payload etc accordingly."

**Status:** ✅ Already added to entity models

### Changes Required:

#### 1.1 Additional Junction Tables

```csharp
// SmartInsights.Domain/Entities/InquiryProgram.cs
namespace SmartInsights.Domain.Entities;

public class InquiryProgram
{
    public Guid InquiryId { get; set; }
    public Guid ProgramId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Program Program { get; set; } = null!;
}

// SmartInsights.Domain/Entities/InquirySemester.cs
namespace SmartInsights.Domain.Entities;

public class InquirySemester
{
    public Guid InquiryId { get; set; }
    public Guid SemesterId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
}
```

#### 1.2 EF Core Configuration

```csharp
// SmartInsights.Infrastructure/Data/Configurations/InquiryConfiguration.cs
public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        // Existing configuration...

        // Configure InquiryDepartments many-to-many
        builder.HasMany(i => i.InquiryDepartments)
            .WithOne(id => id.Inquiry)
            .HasForeignKey(id => id.InquiryId);

        // Configure InquiryPrograms many-to-many
        builder.HasMany(i => i.InquiryPrograms)
            .WithOne(ip => ip.Inquiry)
            .HasForeignKey(ip => ip.InquiryId);

        // Configure InquirySemesters many-to-many
        builder.HasMany(i => i.InquirySemesters)
            .WithOne(iss => iss.Inquiry)
            .HasForeignKey(iss => iss.InquiryId);
    }
}

// SmartInsights.Infrastructure/Data/Configurations/InquiryProgramConfiguration.cs
public class InquiryProgramConfiguration : IEntityTypeConfiguration<InquiryProgram>
{
    public void Configure(EntityTypeBuilder<InquiryProgram> builder)
    {
        builder.HasKey(ip => new { ip.InquiryId, ip.ProgramId });

        builder.HasOne(ip => ip.Inquiry)
            .WithMany(i => i.InquiryPrograms)
            .HasForeignKey(ip => ip.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ip => ip.Program)
            .WithMany()
            .HasForeignKey(ip => ip.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// SmartInsights.Infrastructure/Data/Configurations/InquirySemesterConfiguration.cs
public class InquirySemesterConfiguration : IEntityTypeConfiguration<InquirySemester>
{
    public void Configure(EntityTypeBuilder<InquirySemester> builder)
    {
        builder.HasKey(iss => new { iss.InquiryId, iss.SemesterId });

        builder.HasOne(iss => iss.Inquiry)
            .WithMany(i => i.InquirySemesters)
            .HasForeignKey(iss => iss.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(iss => iss.Semester)
            .WithMany()
            .HasForeignKey(iss => iss.SemesterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 1.3 DTOs

```csharp
// SmartInsights.Application/DTOs/Inquiries/CreateInquiryDto.cs
public class CreateInquiryDto
{
    public string Body { get; set; } = string.Empty;
    public List<Guid> DepartmentIds { get; set; } = new();
    public List<Guid> ProgramIds { get; set; } = new();  // NEW
    public List<Guid> SemesterIds { get; set; } = new(); // NEW
    public InquiryStatus Status { get; set; } = InquiryStatus.Draft;
}

// SmartInsights.Application/DTOs/Inquiries/InquiryDto.cs
public class InquiryDto
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public InquiryStatus Status { get; set; }
    public UserDto CreatedBy { get; set; } = null!;
    public List<DepartmentDto> TargetDepartments { get; set; } = new();
    public List<ProgramDto> TargetPrograms { get; set; } = new();    // NEW
    public List<SemesterDto> TargetSemesters { get; set; } = new(); // NEW
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
```

#### 1.4 API Endpoint Updates

```http
POST /api/inquiries
{
  "body": "How satisfied are you with the lab equipment?",
  "departmentIds": ["dept-1", "dept-2"],
  "programIds": ["prog-1", "prog-2"],     // NEW
  "semesterIds": ["sem-5", "sem-6"],      // NEW
  "status": "Draft"
}

GET /api/inquiries/{id}
{
  ...
  "targetDepartments": [...],
  "targetPrograms": [...],     // NEW
  "targetSemesters": [...]     // NEW
}
```

#### 1.5 Service Layer Updates

```csharp
// SmartInsights.Application/Services/InquiryService.cs
public async Task<Inquiry> CreateInquiryAsync(CreateInquiryDto dto, Guid createdById)
{
    var inquiry = new Inquiry
    {
        Id = Guid.NewGuid(),
        Body = dto.Body,
        Status = dto.Status,
        CreatedById = createdById,
        CreatedAt = DateTime.UtcNow
    };

    await _inquiryRepository.AddAsync(inquiry);

    // Add department relationships
    foreach (var deptId in dto.DepartmentIds)
    {
        await _inquiryDepartmentRepository.AddAsync(new InquiryDepartment
        {
            InquiryId = inquiry.Id,
            DepartmentId = deptId
        });
    }

    // Add program relationships (NEW)
    foreach (var progId in dto.ProgramIds)
    {
        await _inquiryProgramRepository.AddAsync(new InquiryProgram
        {
            InquiryId = inquiry.Id,
            ProgramId = progId
        });
    }

    // Add semester relationships (NEW)
    foreach (var semId in dto.SemesterIds)
    {
        await _inquirySemesterRepository.AddAsync(new InquirySemester
        {
            InquiryId = inquiry.Id,
            SemesterId = semId
        });
    }

    return inquiry;
}
```

---

## Modification 2: InputReply - Separate Entity for Conversations

**Location:** Line 498
**Comment:** "Admin and student can have a conversation in an input so we probably need a separate table for replies something like InputReply"

**Status:** ⚠️ Needs implementation

### Changes Required:

#### 2.1 New Entity

```csharp
// SmartInsights.Domain/Entities/InputReply.cs
namespace SmartInsights.Domain.Entities;

public class InputReply : BaseEntity
{
    public Guid Id { get; set; }
    public Guid InputId { get; set; }
    public Guid UserId { get; set; }
    public Role UserRole { get; set; } // Admin or Student
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Input Input { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

#### 2.2 Update Input Entity

```csharp
// SmartInsights.Domain/Entities/Input.cs
public class Input : BaseEntity
{
    // ... existing properties ...

    // Remove these (commented out in current plan):
    // public string? AdminReply { get; set; }
    // public DateTime? RepliedAt { get; set; }

    // Add navigation property
    public ICollection<InputReply> Replies { get; set; } = new List<InputReply>();

    // ... rest of properties ...
}
```

#### 2.3 EF Core Configuration

```csharp
// SmartInsights.Infrastructure/Data/Configurations/InputReplyConfiguration.cs
public class InputReplyConfiguration : IEntityTypeConfiguration<InputReply>
{
    public void Configure(EntityTypeBuilder<InputReply> builder)
    {
        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(ir => ir.UserRole)
            .HasConversion<string>()
            .IsRequired();

        // Relationships
        builder.HasOne(ir => ir.Input)
            .WithMany(i => i.Replies)
            .HasForeignKey(ir => ir.InputId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ir => ir.User)
            .WithMany()
            .HasForeignKey(ir => ir.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for performance
        builder.HasIndex(ir => ir.InputId);
        builder.HasIndex(ir => new { ir.InputId, ir.CreatedAt });
    }
}
```

#### 2.4 DTOs

```csharp
// SmartInsights.Application/DTOs/Inputs/InputReplyDto.cs
public class InputReplyDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty; // "Admin" or "Student"
    public UserDto User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// SmartInsights.Application/DTOs/Inputs/CreateReplyDto.cs
public class CreateReplyDto
{
    public string Message { get; set; } = string.Empty;
}
```

#### 2.5 API Endpoints

```http
POST /api/inputs/{inputId}/replies
Authorization: Bearer {token}
Content-Type: application/json

{
  "message": "Thank you for your feedback. We are investigating this issue."
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "reply-123",
    "message": "Thank you for your feedback...",
    "userRole": "Admin",
    "user": {
      "id": "admin-1",
      "firstName": "Dr. Ahmed",
      "lastName": "Khan"
    },
    "createdAt": "2024-11-05T10:30:00Z"
  }
}

GET /api/inputs/{inputId}/replies
Authorization: Bearer {token}

Response 200 OK:
{
  "success": true,
  "data": [
    {
      "id": "reply-1",
      "message": "Student message...",
      "userRole": "Student",
      "user": { ... },
      "createdAt": "2024-11-05T10:00:00Z"
    },
    {
      "id": "reply-2",
      "message": "Admin reply...",
      "userRole": "Admin",
      "user": { ... },
      "createdAt": "2024-11-05T10:30:00Z"
    }
  ]
}
```

#### 2.6 Service Layer

```csharp
// SmartInsights.Application/Interfaces/IInputReplyService.cs
public interface IInputReplyService
{
    Task<InputReply> CreateReplyAsync(Guid inputId, CreateReplyDto dto, Guid userId, Role userRole);
    Task<List<InputReply>> GetRepliesByInputIdAsync(Guid inputId);
}

// SmartInsights.Application/Services/InputReplyService.cs
public class InputReplyService : IInputReplyService
{
    private readonly IRepository<InputReply> _replyRepository;
    private readonly IRepository<Input> _inputRepository;
    private readonly IRepository<User> _userRepository;

    public InputReplyService(
        IRepository<InputReply> replyRepository,
        IRepository<Input> inputRepository,
        IRepository<User> userRepository)
    {
        _replyRepository = replyRepository;
        _inputRepository = inputRepository;
        _userRepository = userRepository;
    }

    public async Task<InputReply> CreateReplyAsync(
        Guid inputId,
        CreateReplyDto dto,
        Guid userId,
        Role userRole)
    {
        // Validate input exists
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null)
            throw new NotFoundException("Input not found");

        // If student, verify ownership
        if (userRole == Role.Student && input.UserId != userId)
            throw new UnauthorizedAccessException("Cannot reply to others' inputs");

        var reply = new InputReply
        {
            Id = Guid.NewGuid(),
            InputId = inputId,
            UserId = userId,
            UserRole = userRole,
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow
        };

        await _replyRepository.AddAsync(reply);
        return reply;
    }

    public async Task<List<InputReply>> GetRepliesByInputIdAsync(Guid inputId)
    {
        return await _replyRepository.FindAsync(
            filter: r => r.InputId == inputId,
            orderBy: q => q.OrderBy(r => r.CreatedAt),
            include: q => q.Include(r => r.User));
    }
}
```

#### 2.7 Controller

```csharp
// SmartInsights.API/Controllers/InputsController.cs (partial)
[HttpPost("{inputId}/replies")]
[Authorize]
public async Task<IActionResult> CreateReply(
    Guid inputId,
    [FromBody] CreateReplyDto dto)
{
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var userRole = Enum.Parse<Role>(User.FindFirst(ClaimTypes.Role)!.Value);

    var reply = await _inputReplyService.CreateReplyAsync(inputId, dto, userId, userRole);

    return CreatedAtAction(
        nameof(GetReplies),
        new { inputId },
        new { success = true, data = reply });
}

[HttpGet("{inputId}/replies")]
[Authorize]
public async Task<IActionResult> GetReplies(Guid inputId)
{
    // Verify access (admin can see all, student only own)
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var userRole = Enum.Parse<Role>(User.FindFirst(ClaimTypes.Role)!.Value);

    if (userRole == Role.Student)
    {
        var input = await _inputService.GetInputByIdAsync(inputId);
        if (input == null || input.UserId != userId)
            return Forbid();
    }

    var replies = await _inputReplyService.GetRepliesByInputIdAsync(inputId);
    return Ok(new { success = true, data = replies });
}
```

#### 2.8 Validation

```csharp
// SmartInsights.Application/Validators/CreateReplyValidator.cs
public class CreateReplyValidator : AbstractValidator<CreateReplyDto>
{
    public CreateReplyValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Reply message is required")
            .MinimumLength(1).WithMessage("Reply must not be empty")
            .MaximumLength(2000).WithMessage("Reply cannot exceed 2000 characters");
    }
}
```

---

## Modification 3: Department-Program-Semester Hierarchy (Future Enhancement)

**Location:** Line 625
**Comment:** "I believe we can improve relationships of departments, programs and semesters. Each department has several programs and each program has semesters and each semester has students enrolled in it."

**Status:** 📝 For future consideration (keep simple for MVP)

### Current Structure (MVP):
```
User ─┬─→ Department
      ├─→ Program
      └─→ Semester
```

### Proposed Future Structure:
```
Department ─→ Program ─→ Semester ─→ User
```

### Implementation Notes:

**Pros of Hierarchy**:
- More realistic data model
- Better data integrity
- Easier filtering (e.g., "all semesters in a program")
- Prevents invalid combinations (e.g., semester 8 with department that doesn't have 8 semesters)

**Cons for MVP**:
- More complex setup
- More foreign key relationships
- Harder to migrate data
- Need to populate program-semester relationships

**Recommendation**: Implement in Phase 2 after MVP is stable.

### Future Schema (Reference):

```csharp
// Future: Department has many Programs
public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ICollection<Program> Programs { get; set; }
}

// Future: Program belongs to Department, has many Semesters
public class Program
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public ICollection<Semester> Semesters { get; set; }
}

// Future: Semester belongs to Program, has many Users
public class Semester
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public Guid ProgramId { get; set; }
    public Program Program { get; set; }
    public ICollection<User> Users { get; set; }
}

// Future: User belongs to Semester (and inherits Department + Program)
public class User
{
    public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; }

    // Computed properties
    public Program Program => Semester?.Program;
    public Department Department => Semester?.Program?.Department;
}
```

---

## Modification 4: Separate Inputs Endpoint

**Location:** Line 1434
**Comment:** "I think it would be better to have a separate endpoint for inputs in general and we can pass parameters like inquiryId, topicId to distinguish between different input types."

**Status:** ✅ Good suggestion - implement this

### Changes Required:

#### 4.1 New Endpoint

```http
GET /api/inputs
Authorization: Bearer {token} (Admin only)

Query Parameters:
- page (int, default: 1)
- pageSize (int, default: 20)
- type (string, optional: "General" | "InquiryLinked")
- inquiryId (guid, optional: filter by inquiry)
- topicId (guid, optional: filter by topic)
- departmentId (guid, optional: filter by department)
- sentiment (string, optional: filter by sentiment)
- minQuality (double, optional: 0.0-1.0)
- severity (int, optional: 1-3)
- status (string, optional: "Pending" | "Processing" | "Processed")
- sortBy (string: "createdAt" | "score" | "severity", default: "createdAt")
- sortOrder (string: "asc" | "desc", default: "desc")
- search (string, optional: search in body text)

Response 200 OK:
{
  "success": true,
  "data": {
    "inputs": [
      {
        "id": "input-123",
        "body": "Feedback text...",
        "type": "General",
        "sentiment": "Negative",
        "tone": "Negative",
        "qualityMetrics": {
          "urgency": 0.85,
          "importance": 0.90,
          "clarity": 0.95,
          "quality": 0.88,
          "helpfulness": 0.92,
          "score": 0.90,
          "severity": 3
        },
        "inquiry": null,
        "topic": {
          "id": "topic-5",
          "name": "WiFi Connectivity Issues"
        },
        "user": {
          "department": "Computer Science",
          "program": "Computer Science",
          "semester": "6",
          "isRevealed": false
        },
        "status": "Processed",
        "hasReplies": true,
        "replyCount": 3,
        "revealRequested": true,
        "revealApproved": null,
        "createdAt": "2024-11-05T10:00:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalPages": 15,
      "totalCount": 287
    },
    "filters": {
      "totalInputs": 287,
      "byType": {
        "General": 145,
        "InquiryLinked": 142
      },
      "bySeverity": {
        "High": 85,
        "Medium": 120,
        "Low": 82
      },
      "byStatus": {
        "Pending": 12,
        "Processing": 5,
        "Processed": 270
      }
    }
  }
}
```

#### 4.2 Service Layer

```csharp
// SmartInsights.Application/DTOs/Inputs/InputFilterDto.cs
public class InputFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public InputType? Type { get; set; }
    public Guid? InquiryId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Sentiment? Sentiment { get; set; }
    public double? MinQuality { get; set; }
    public int? Severity { get; set; }
    public InputStatus? Status { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortOrder { get; set; } = "desc";
    public string? Search { get; set; }
}

// SmartInsights.Application/Services/InputService.cs (add method)
public async Task<PagedResult<Input>> GetInputsAsync(InputFilterDto filter)
{
    var query = _context.Inputs
        .Include(i => i.User)
            .ThenInclude(u => u.Department)
        .Include(i => i.Inquiry)
        .Include(i => i.Topic)
        .Include(i => i.Theme)
        .Include(i => i.Replies)
        .AsQueryable();

    // Apply filters
    if (filter.Type.HasValue)
        query = query.Where(i => i.Type == filter.Type.Value);

    if (filter.InquiryId.HasValue)
        query = query.Where(i => i.InquiryId == filter.InquiryId.Value);

    if (filter.TopicId.HasValue)
        query = query.Where(i => i.TopicId == filter.TopicId.Value);

    if (filter.DepartmentId.HasValue)
        query = query.Where(i => i.User.DepartmentId == filter.DepartmentId.Value);

    if (filter.Sentiment.HasValue)
        query = query.Where(i => i.Sentiment == filter.Sentiment.Value);

    if (filter.MinQuality.HasValue)
        query = query.Where(i => i.Score >= filter.MinQuality.Value);

    if (filter.Severity.HasValue)
        query = query.Where(i => i.Severity == filter.Severity.Value);

    if (filter.Status.HasValue)
        query = query.Where(i => i.Status == filter.Status.Value);

    if (!string.IsNullOrWhiteSpace(filter.Search))
        query = query.Where(i => i.Body.Contains(filter.Search));

    // Count before pagination
    var totalCount = await query.CountAsync();

    // Apply sorting
    query = filter.SortBy.ToLower() switch
    {
        "score" => filter.SortOrder == "asc"
            ? query.OrderBy(i => i.Score)
            : query.OrderByDescending(i => i.Score),
        "severity" => filter.SortOrder == "asc"
            ? query.OrderBy(i => i.Severity)
            : query.OrderByDescending(i => i.Severity),
        _ => filter.SortOrder == "asc"
            ? query.OrderBy(i => i.CreatedAt)
            : query.OrderByDescending(i => i.CreatedAt)
    };

    // Apply pagination
    var inputs = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();

    return new PagedResult<Input>
    {
        Items = inputs,
        CurrentPage = filter.Page,
        PageSize = filter.PageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
    };
}
```

#### 4.3 Controller

```csharp
// SmartInsights.API/Controllers/InputsController.cs
[HttpGet]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> GetInputs([FromQuery] InputFilterDto filter)
{
    var result = await _inputService.GetInputsAsync(filter);

    // Calculate filter stats
    var filterStats = new
    {
        totalInputs = result.TotalCount,
        byType = new
        {
            General = await _inputService.CountByTypeAsync(InputType.General),
            InquiryLinked = await _inputService.CountByTypeAsync(InputType.InquiryLinked)
        },
        bySeverity = new
        {
            High = await _inputService.CountBySeverityAsync(3),
            Medium = await _inputService.CountBySeverityAsync(2),
            Low = await _inputService.CountBySeverityAsync(1)
        },
        byStatus = new
        {
            Pending = await _inputService.CountByStatusAsync(InputStatus.Pending),
            Processing = await _inputService.CountByStatusAsync(InputStatus.Processing),
            Processed = await _inputService.CountByStatusAsync(InputStatus.Processed)
        }
    };

    return Ok(new
    {
        success = true,
        data = new
        {
            inputs = result.Items,
            pagination = new
            {
                result.CurrentPage,
                result.PageSize,
                result.TotalPages,
                result.TotalCount
            },
            filters = filterStats
        }
    });
}
```

#### 4.4 Update Inquiry/Topic Detail Endpoints

Now that we have a separate inputs endpoint, we can simplify the inquiry/topic detail responses:

```http
GET /api/inquiries/{id}
Response (simplified - no inputs array):
{
  "success": true,
  "data": {
    "id": "inq-123",
    "body": "Question...",
    "status": "Active",
    "stats": {
      "totalResponses": 245,
      "averageQuality": 0.73,
      "sentimentBreakdown": { ... }
    },
    "aiSummary": { ... },
    "createdAt": "2024-10-15T10:00:00Z"
  }
}

To get inputs for this inquiry, use:
GET /api/inputs?inquiryId={id}&page=1&pageSize=20

Similarly for topics:
GET /api/inputs?topicId={id}&page=1&pageSize=20
```

---

## Modification 5: Update Inquiry Creation Endpoint Documentation

**Location:** Line 1453
**Comment:** "We discussed above that each inquiry can have further tags like programs and semesters so handle it."

**Status:** ✅ Already covered in Modification 1

See Modification 1 for complete details on programs and semesters support.

---

## Modification 6: Remove Admin Reply from Input Entity

**Location:** Lines 1719, 3231
**Comment:** "We discussed above that InputReply is gonna be a separate entity so it needs to be handled separately."

**Status:** ✅ Already covered in Modification 2

### Actions Required:

1. Remove `AdminReply` and `RepliedAt` properties from Input entity ✅
2. Create InputReply entity ✅
3. Update all DTOs to remove adminReply field ✅
4. Update API endpoints to use /replies endpoint ✅
5. Update Input detail response to include reply count instead of single reply ✅

---

## Modification 7: Skip Inputs in Inquiry/Topic Detail Response

**Location:** Line 1953
**Comment:** "We discussed above that Inputs will be handled separately so we can skip it here."

**Status:** ✅ Covered in Modification 4

### Updated Response Format:

```http
GET /api/inquiries/{id}
{
  "success": true,
  "data": {
    "id": "inq-123",
    "body": "Question...",
    "status": "Active",
    "createdBy": { ... },
    "targetDepartments": [ ... ],
    "targetPrograms": [ ... ],
    "targetSemesters": [ ... ],
    "aiSummary": { ... },
    "stats": {
      "totalResponses": 245,
      "averageQuality": 0.73,
      "sentimentBreakdown": { ... },
      "severityBreakdown": { ... }
    },
    "createdAt": "2024-10-15T10:00:00Z",
    "sentAt": "2024-10-15T14:00:00Z"
  },
  "message": "To view inputs, use GET /api/inputs?inquiryId={id}"
}
```

---

## Migration Path

### Step 1: Database Changes
```bash
# Create new migration
dotnet ef migrations add AddInputRepliesAndInquiryTargeting

# Review migration
# Apply migration
dotnet ef database update
```

### Step 2: Update Entity Models
- Add InquiryProgram and InquirySemester entities
- Add InputReply entity
- Update Input entity (remove admin reply fields)
- Update Inquiry entity (add program/semester collections)

### Step 3: Update Configurations
- Add EF Core configurations for new entities
- Update existing configurations

### Step 4: Update Services
- Create InputReplyService
- Update InquiryService to handle programs/semesters
- Update InputService to add filter method

### Step 5: Update Controllers
- Add InputReplyController or update InputsController with reply endpoints
- Update InquiriesController to handle programs/semesters
- Add/update inputs list endpoint with filtering

### Step 6: Update DTOs
- Create InputReplyDto and related DTOs
- Update CreateInquiryDto with program/semester IDs
- Update InquiryDto with program/semester info
- Create InputFilterDto

### Step 7: Testing
- Test inquiry creation with programs/semesters
- Test conversation flow (multiple replies)
- Test input filtering with various parameters
- Integration testing for all endpoints

---

## Summary Checklist

### Modification 1: Inquiry Targeting ✅
- [x] InquiryProgram entity
- [x] InquirySemester entity
- [x] EF Core configurations
- [x] Update DTOs
- [x] Update API endpoints
- [x] Update service layer

### Modification 2: InputReply Entity ⚠️
- [ ] Create InputReply entity
- [ ] Remove AdminReply from Input
- [ ] EF Core configuration
- [ ] Create DTOs
- [ ] Add /replies endpoints
- [ ] InputReplyService
- [ ] Update controllers
- [ ] Add validation

### Modification 3: Hierarchy (Future) 📝
- [ ] Design detailed schema
- [ ] Plan migration strategy
- [ ] Implement in Phase 2

### Modification 4: Separate Inputs Endpoint ⚠️
- [ ] Create InputFilterDto
- [ ] Add filtering logic to service
- [ ] Create GET /api/inputs endpoint
- [ ] Update inquiry/topic responses
- [ ] Add filter stats

### Modification 5: Inquiry Programs/Semesters ✅
- [x] Same as Modification 1

### Modification 6: Remove AdminReply ⚠️
- [ ] Same as Modification 2

### Modification 7: Skip Inputs in Detail ⚠️
- [ ] Same as Modification 4

---

## Priority Order

**High Priority (MVP Critical)**:
1. ✅ Inquiry targeting with programs/semesters (Mod 1) - Already in plan
2. ⚠️ InputReply entity for conversations (Mod 2) - Implement next
3. ⚠️ Separate inputs endpoint with filtering (Mod 4) - Implement next

**Medium Priority (Nice to Have)**:
4. Department-Program-Semester hierarchy (Mod 3) - Phase 2

---

## Next Steps

1. Review this document with team
2. Prioritize modifications
3. Update DOTNET_BACKEND_PLAN.md with details from Modifications 2 and 4
4. Create migration for new entities
5. Begin implementation

---

**Document Version:** 1.0
**Last Updated:** November 5, 2024
