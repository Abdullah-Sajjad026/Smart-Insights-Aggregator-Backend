# Smart Insights Aggregator - .NET 8 Backend Implementation Plan

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Database Design](#database-design)
5. [Entity Models](#entity-models)
6. [API Endpoints](#api-endpoints)
7. [Service Layer](#service-layer)
8. [AI Integration](#ai-integration)
9. [Authentication & Authorization](#authentication--authorization)
10. [Implementation Phases](#implementation-phases)

---

## Project Overview

### System Purpose

Smart Insights Aggregator is a single-tenant feedback and inquiry management system for KFUEIT University. It enables:

- **Students**: Submit anonymous feedback (general inputs) and respond to admin inquiries
- **Admins**: Create inquiries, view responses, analyze feedback through AI-powered insights, track emerging topics

### Core Features

- Anonymous student feedback submission
- Admin-created inquiries with targeted responses
- AI-powered classification, quality scoring, and topic grouping
- Aggregate AI analysis with executive summaries
- Admin-student interaction (replies, identity reveal requests)
- Topic discovery and trend analysis

### Key Terminology

- **Inquiry**: Admin-created question/prompt that students respond to
- **Input**: Student-submitted feedback (two types: INQUIRY_LINKED or GENERAL)
- **Topic**: AI-generated grouping of related general inputs
- **Quality Metrics**: AI-calculated scores (urgency, importance, clarity, quality, helpfulness)
- **Executive Summary**: Structured AI analysis of inputs at inquiry/topic level

---

## Technology Stack

### Core Framework

- **.NET 8.0** - Latest LTS version
- **ASP.NET Core Web API** - RESTful API framework
- **C# 12** - Latest language features

### Database & ORM

- **PostgreSQL 16** - Primary database
- **Entity Framework Core 8** - ORM and migrations
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider

### Authentication & Security

- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication
- **BCrypt.Net-Next** - Password hashing
- **System.IdentityModel.Tokens.Jwt** - JWT token generation

### AI Integration

- **Azure.AI.OpenAI** (v2.0+) - Azure OpenAI SDK (Primary choice)
- Alternative: **Google.Ai.Generativelanguage** - Google Gemini

### Background Processing

- **Hangfire** - Background job processing for AI tasks
- **Hangfire.PostgreSql** - PostgreSQL storage for Hangfire

### Utilities

- **AutoMapper** - DTO mapping
- **FluentValidation** - Request validation
- **Serilog** - Structured logging
- **Swashbuckle.AspNetCore** - API documentation (Swagger)

### Testing (Optional for MVP)

- **xUnit** - Testing framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library

---

## Architecture

### Clean Architecture Pattern

```
SmartInsights/
├── src/
│   ├── SmartInsights.Domain/              # Core business entities
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Inquiry.cs
│   │   │   ├── Input.cs
│   │   │   ├── Topic.cs
│   │   │   ├── Department.cs
│   │   │   ├── Program.cs
│   │   │   └── Semester.cs
│   │   ├── Enums/
│   │   │   ├── Role.cs
│   │   │   ├── UserStatus.cs
│   │   │   ├── Sentiment.cs
│   │   │   ├── Tone.cs
│   │   │   ├── InputStatus.cs
│   │   │   ├── InputType.cs
│   │   │   └── InquiryStatus.cs
│   │   └── Common/
│   │       └── BaseEntity.cs
│   │
│   ├── SmartInsights.Application/         # Business logic & interfaces
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   ├── Inquiries/
│   │   │   ├── Inputs/
│   │   │   ├── Topics/
│   │   │   └── Users/
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IInquiryService.cs
│   │   │   ├── IInputService.cs
│   │   │   ├── ITopicService.cs
│   │   │   ├── IUserService.cs
│   │   │   ├── IAiProcessingService.cs
│   │   │   └── IRepository.cs
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── InquiryService.cs
│   │   │   ├── InputService.cs
│   │   │   ├── TopicService.cs
│   │   │   └── UserService.cs
│   │   ├── Validators/
│   │   ├── Mappings/
│   │   └── Common/
│   │
│   ├── SmartInsights.Infrastructure/      # External concerns
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   └── Repository.cs
│   │   ├── AI/
│   │   │   ├── AzureOpenAiService.cs
│   │   │   ├── Prompts/
│   │   │   └── Models/
│   │   ├── BackgroundJobs/
│   │   │   └── InputProcessingJob.cs
│   │   └── Persistence/
│   │
│   └── SmartInsights.API/                 # Web API layer
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── InquiriesController.cs
│       │   ├── InputsController.cs
│       │   ├── TopicsController.cs
│       │   ├── UsersController.cs
│       │   └── DashboardController.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/                                  # Test projects (optional)
```

### Layer Responsibilities

#### Domain Layer

- Pure business entities
- No dependencies on other layers
- Enums and domain-specific types
- Business rules and invariants

#### Application Layer

- Business logic and use cases
- Service interfaces and implementations
- DTOs for data transfer
- Validators for business rules
- AutoMapper profiles

#### Infrastructure Layer

- Database context and repositories
- External service integrations (AI)
- Background job implementations
- Caching, logging, email services

#### API Layer

- HTTP endpoints (controllers)
- Request/response handling
- Authentication/authorization
- Middleware for cross-cutting concerns
- Dependency injection configuration

---

## Database Design

### Entity Relationship Diagram

```
┌─────────────────┐
│     User        │
├─────────────────┤
│ Id (PK)         │
│ Email           │
│ FirstName       │
│ LastName        │
│ PasswordHash    │
│ Role            │
│ Status          │
│ DepartmentId FK │
│ ProgramId FK    │
│ SemesterId FK   │
│ CreatedAt       │
│ UpdatedAt       │
└────────┬────────┘
         │
         │ 1:N
         ├──────────────────────┐
         │                      │
         ▼                      ▼
┌─────────────────┐    ┌──────────────────┐
│   Inquiry       │    │      Input       │
├─────────────────┤    ├──────────────────┤
│ Id (PK)         │◄───│ Id (PK)          │
│ Body            │ 1:N│ Body             │
│ Status          │    │ Type             │
│ CreatedById FK  │    │ Status           │
│ CreatedAt       │    │ UserId FK        │
│ SentAt          │    │ InquiryId FK     │
│ ClosedAt        │    │ TopicId FK       │
└────────┬────────┘    │ ThemeId FK       │
         │             │ Sentiment        │
         │             │ Tone             │
         │             │ UrgencyPct       │
         │             │ ImportancePct    │
         │             │ ClarityPct       │
         │             │ QualityPct       │
         │             │ HelpfulnessPct   │
         │             │ Score            │
         │             │ Severity         │
         │             │ AdminReply       │
         │             │ RepliedAt        │
         │             │ RevealRequested  │
         │             │ RevealApproved   │
         │             │ CreatedAt        │
         │             │ UpdatedAt        │
         │             └────────┬─────────┘
         │                      │
         │                      │ N:1
         │                      ▼
         │             ┌──────────────────┐
         │             │      Topic       │
         │             ├──────────────────┤
         │             │ Id (PK)          │
         │             │ Name             │
         │             │ DepartmentId FK  │
         │             │ Summary (JSON)   │
         │             │ SummaryGenAt     │
         │             │ CreatedAt        │
         │             │ UpdatedAt        │
         │             └──────────────────┘
         │
         │ N:M
         ▼
┌─────────────────────┐
│ InquiryDepartment   │
├─────────────────────┤
│ InquiryId FK (PK)   │
│ DepartmentId FK (PK)│
└─────────────────────┘
         │
         ▼
┌─────────────────┐
│   Department    │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ Description     │
│ CreatedAt       │
└─────────────────┘

┌─────────────────┐
│    Program      │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ CreatedAt       │
└─────────────────┘

┌─────────────────┐
│   Semester      │
├─────────────────┤
│ Id (PK)         │
│ Value           │
│ CreatedAt       │
└─────────────────┘

┌─────────────────┐
│     Theme       │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ CreatedAt       │
└─────────────────┘
```

### Database Indexes

```sql
-- User indexes
CREATE INDEX idx_user_email ON "User" (Email);
CREATE INDEX idx_user_role ON "User" (Role);
CREATE INDEX idx_user_department ON "User" (DepartmentId);

-- Input indexes
CREATE INDEX idx_input_user ON "Input" (UserId);
CREATE INDEX idx_input_inquiry ON "Input" (InquiryId);
CREATE INDEX idx_input_topic ON "Input" (TopicId);
CREATE INDEX idx_input_status ON "Input" (Status);
CREATE INDEX idx_input_type ON "Input" (Type);
CREATE INDEX idx_input_sentiment ON "Input" (Sentiment);
CREATE INDEX idx_input_created ON "Input" (CreatedAt DESC);

-- Topic indexes
CREATE INDEX idx_topic_department ON "Topic" (DepartmentId);
CREATE INDEX idx_topic_name ON "Topic" (Name);

-- Inquiry indexes
CREATE INDEX idx_inquiry_status ON "Inquiry" (Status);
CREATE INDEX idx_inquiry_created ON "Inquiry" (CreatedAt DESC);
```

---

## Entity Models

### User Entity

```csharp
// SmartInsights.Domain/Entities/User.cs
using SmartInsights.Domain.Enums;

namespace SmartInsights.Domain.Entities;

public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Student;
    public UserStatus Status { get; set; } = UserStatus.Active;

    // Student-specific fields (nullable for admins)
    public Guid? DepartmentId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? SemesterId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Department? Department { get; set; }
    public Program? Program { get; set; }
    public Semester? Semester { get; set; }
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
    public ICollection<Inquiry> CreatedInquiries { get; set; } = new List<Inquiry>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
```

### Inquiry Entity

```csharp
// SmartInsights.Domain/Entities/Inquiry.cs
using SmartInsights.Domain.Enums;
using System.Text.Json;

namespace SmartInsights.Domain.Entities;

public class Inquiry : BaseEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty; // The question/prompt
    public InquiryStatus Status { get; set; } = InquiryStatus.Draft;

    // Creator info
    public Guid CreatedById { get; set; }

    // AI-generated summary (stored as JSON)
    public string? Summary { get; set; } // ExecutiveSummary JSON
    public DateTime? SummaryGeneratedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<InquiryDepartment> InquiryDepartments { get; set; } = new List<InquiryDepartment>();
    // Modify: An Inquiry will also have InquiryPrograms, and InquirySemesters as well so need to update it and anything related like dto, payload etc accordingly.
    public ICollection<InquiryProgram> InquiryPrograms { get; set; } = new List<InquiryProgram>();
    public ICollection<InquirySemester> InquirySemesters { get; set; } = new List<InquirySemester>();

    public ICollection<Input> Inputs { get; set; } = new List<Input>();

    // Helper methods
    public ExecutiveSummary? GetParsedSummary()
    {
        if (string.IsNullOrEmpty(Summary)) return null;
        return JsonSerializer.Deserialize<ExecutiveSummary>(Summary);
    }

    public void SetSummary(ExecutiveSummary summary)
    {
        Summary = JsonSerializer.Serialize(summary);
        SummaryGeneratedAt = DateTime.UtcNow;
    }
}

// DTO for AI Summary
public class ExecutiveSummary
{
    public List<string> Topics { get; set; } = new();
    public Dictionary<string, string> ExecutiveSummaryData { get; set; } = new();
    public List<SuggestedAction> SuggestedPrioritizedActions { get; set; } = new();
}

public class SuggestedAction
{
    public string Action { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Challenges { get; set; } = string.Empty;
    public int ResponseCount { get; set; }
    public string SupportingReasoning { get; set; } = string.Empty;
}
```

### Input Entity

```csharp
// SmartInsights.Domain/Entities/Input.cs
using SmartInsights.Domain.Enums;

namespace SmartInsights.Domain.Entities;

public class Input : BaseEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public InputType Type { get; set; } // GENERAL or INQUIRY_LINKED
    public InputStatus Status { get; set; } = InputStatus.Pending;

    // Foreign keys
    public Guid UserId { get; set; }
    public Guid? InquiryId { get; set; } // Null for GENERAL inputs
    public Guid? TopicId { get; set; } // For GENERAL inputs after classification
    public Guid? ThemeId { get; set; } // For GENERAL inputs

    // AI Analysis Results
    public Sentiment? Sentiment { get; set; }
    public Tone? Tone { get; set; }

    // Quality Metrics (0.0 to 1.0)
    public double? UrgencyPct { get; set; }
    public double? ImportancePct { get; set; }
    public double? ClarityPct { get; set; }
    public double? QualityPct { get; set; }
    public double? HelpfulnessPct { get; set; }

    // Derived metrics
    public double? Score { get; set; } // Calculated from quality metrics
    public int? Severity { get; set; } // 1=LOW, 2=MEDIUM, 3=HIGH

    // Admin interaction
    // public string? AdminReply { get; set; }
    // public DateTime? RepliedAt { get; set; }
    // Modify: Admin and student can have a conversation in an input so we probably need a separate table for replies something like InputReply

    public bool RevealRequested { get; set; } = false;
    public bool? RevealApproved { get; set; } // null=pending, true=approved, false=denied

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Inquiry? Inquiry { get; set; }
    public Topic? Topic { get; set; }
    public Theme? Theme { get; set; }

    // Helper methods
    public void CalculateScore()
    {
        if (UrgencyPct.HasValue && ImportancePct.HasValue &&
            ClarityPct.HasValue && QualityPct.HasValue && HelpfulnessPct.HasValue)
        {
            Score = (UrgencyPct.Value + ImportancePct.Value + ClarityPct.Value +
                     QualityPct.Value + HelpfulnessPct.Value) / 5.0;

            // Calculate severity
            if (Score >= 0.75) Severity = 3; // HIGH
            else if (Score >= 0.5) Severity = 2; // MEDIUM
            else Severity = 1; // LOW
        }
    }

    public bool IsAnonymous => !RevealApproved.HasValue || RevealApproved.Value == false;
}
```

### Topic Entity

```csharp
// SmartInsights.Domain/Entities/Topic.cs
using System.Text.Json;

namespace SmartInsights.Domain.Entities;

public class Topic : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // AI-generated (max 5 words)
    public Guid? DepartmentId { get; set; }

    // AI-generated summary (stored as JSON)
    public string? Summary { get; set; } // ExecutiveSummary JSON
    public DateTime? SummaryGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<Input> Inputs { get; set; } = new List<Input>();

    // Helper methods
    public ExecutiveSummary? GetParsedSummary()
    {
        if (string.IsNullOrEmpty(Summary)) return null;
        return JsonSerializer.Deserialize<ExecutiveSummary>(Summary);
    }

    public void SetSummary(ExecutiveSummary summary)
    {
        Summary = JsonSerializer.Serialize(summary);
        SummaryGeneratedAt = DateTime.UtcNow;
    }
}
```

### Department Entity

```csharp
// SmartInsights.Domain/Entities/Department.cs
namespace SmartInsights.Domain.Entities;

public class Department : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<InquiryDepartment> InquiryDepartments { get; set; } = new List<InquiryDepartment>();
}
```

### Program Entity

```csharp
// SmartInsights.Domain/Entities/Program.cs
namespace SmartInsights.Domain.Entities;

public class Program : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Computer Science"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

### Semester Entity

```csharp
// SmartInsights.Domain/Entities/Semester.cs
namespace SmartInsights.Domain.Entities;

public class Semester : BaseEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty; // "1", "2", "3"... "8"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
// Modify: I believe we can improve relationships of departments, programs and semesters. Each department has several programs and each program has semesters and each semester has students enrolled in it. So we can have Department->Program->Semester hierarchy instead of linking users directly to all three. But for MVP we can keep it simple as is.
```

### Theme Entity

```csharp
// SmartInsights.Domain/Entities/Theme.cs
namespace SmartInsights.Domain.Entities;

public class Theme : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Predefined themes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Input> Inputs { get; set; } = new List<Input>();
}
```

### Junction Table - InquiryDepartment

```csharp
// SmartInsights.Domain/Entities/InquiryDepartment.cs
namespace SmartInsights.Domain.Entities;

public class InquiryDepartment
{
    public Guid InquiryId { get; set; }
    public Guid DepartmentId { get; set; }

    // Navigation properties
    public Inquiry Inquiry { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
```

### Base Entity

```csharp
// SmartInsights.Domain/Common/BaseEntity.cs
namespace SmartInsights.Domain.Common;

public abstract class BaseEntity
{
    // Reserved for common audit fields if needed
    // e.g., CreatedBy, ModifiedBy, IsDeleted, etc.
}
```

### Enums

```csharp
// SmartInsights.Domain/Enums/Role.cs
namespace SmartInsights.Domain.Enums;

public enum Role
{
    Admin,
    Student
}

// SmartInsights.Domain/Enums/UserStatus.cs
public enum UserStatus
{
    Active,
    Inactive
}

// SmartInsights.Domain/Enums/Sentiment.cs
public enum Sentiment
{
    Positive,
    Neutral,
    Negative
}

// SmartInsights.Domain/Enums/Tone.cs
public enum Tone
{
    Positive,
    Neutral,
    Negative
}

// SmartInsights.Domain/Enums/InputStatus.cs
public enum InputStatus
{
    Pending,      // Awaiting AI processing
    Processing,   // Currently being processed
    Processed,    // AI analysis complete
    Reviewed,     // Admin viewed
    Error         // Processing failed
}

// SmartInsights.Domain/Enums/InputType.cs
public enum InputType
{
    General,        // Student-initiated feedback
    InquiryLinked   // Response to admin inquiry
}

// SmartInsights.Domain/Enums/InquiryStatus.cs
public enum InquiryStatus
{
    Draft,   // Not sent yet
    Active,  // Sent and accepting responses
    Closed   // No longer accepting responses
}
```

---

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/login

**Purpose**: Authenticate user and return JWT token

**Request Body**:

```json
{
  "email": "student@kfueit.edu.pk",
  "password": "SecurePassword123!"
}
```

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-11-05T15:30:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "student@kfueit.edu.pk",
      "firstName": "Ali",
      "lastName": "Hassan",
      "role": "Student",
      "department": {
        "id": "abc123",
        "name": "Computer Science"
      },
      "program": {
        "id": "def456",
        "name": "BS Computer Science"
      },
      "semester": {
        "id": "ghi789",
        "value": "6"
      }
    }
  }
}
```

**Response (401 Unauthorized)**:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid email or password"
  }
}
```

**Validation Rules**:

- Email: Required, valid email format
- Password: Required, minimum 8 characters

---

#### POST /api/auth/logout

**Purpose**: Invalidate user session (optional - JWT is stateless)

**Headers**:

```
Authorization: Bearer {token}
```

**Response (200 OK)**:

```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

#### GET /api/auth/me

**Purpose**: Get current authenticated user details

**Headers**:

```
Authorization: Bearer {token}
```

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "student@kfueit.edu.pk",
    "firstName": "Ali",
    "lastName": "Hassan",
    "role": "Student",
    "status": "Active",
    "department": {
      "id": "abc123",
      "name": "Computer Science"
    },
    "program": {
      "id": "def456",
      "name": "BS Computer Science"
    },
    "semester": {
      "id": "ghi789",
      "value": "6"
    }
  }
}
```

---

### Workspace Management Endpoints (Admin Only)

#### GET /api/workspace/setup-status

**Purpose**: Check if workspace (departments, programs, semesters) is configured

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "isSetup": true,
    "departmentCount": 5,
    "programCount": 8,
    "semesterCount": 8,
    "userCount": 450
  }
}
```

---

#### GET /api/departments

**Purpose**: Get all departments

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    {
      "id": "dept-1",
      "name": "Computer Science",
      "description": "Computer Science and Software Engineering"
    },
    {
      "id": "dept-2",
      "name": "Electrical Engineering",
      "description": "Electrical and Electronics Engineering"
    }
  ]
}
```

---

#### POST /api/departments

**Purpose**: Create new department

**Authorization**: Admin only

**Request Body**:

```json
{
  "name": "Computer Science",
  "description": "Computer Science and Software Engineering"
}
```

**Response (201 Created)**:

```json
{
  "success": true,
  "data": {
    "id": "dept-1",
    "name": "Computer Science",
    "description": "Computer Science and Software Engineering"
  }
}
```

---

#### POST /api/departments/bulk-create

**Purpose**: Create multiple departments at once (for initial setup)

**Authorization**: Admin only

**Request Body**:

```json
{
  "departments": [
    { "name": "Computer Science", "description": "CS and SE" },
    { "name": "Electrical Engineering", "description": "EE" },
    { "name": "Mechanical Engineering", "description": "ME" },
    { "name": "Civil Engineering", "description": "CE" },
    { "name": "Management Sciences", "description": "MBA/BBA" }
  ]
}
```

**Response (201 Created)**:

```json
{
  "success": true,
  "data": {
    "created": 5,
    "departments": [{ "id": "dept-1", "name": "Computer Science" }]
  }
}
```

---

#### GET /api/programs

**Purpose**: Get all programs

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    {
      "id": "prog-1",
      "name": "Computer Science"
    },
    {
      "id": "prog-2",
      "name": "Software Engineering"
    }
  ]
}
```

---

#### POST /api/programs/bulk-create

**Purpose**: Create multiple programs at once (for initial setup)

**Authorization**: Admin only

**Request Body**:

```json
{
  "programs": [
    "Computer Science",
    "Software Engineering",
    "Information Technology",
    "Electrical Engineering",
    "Mechanical Engineering",
    "Civil Engineering",
    "Business Administration",
    "Management Sciences"
  ]
}
```

**Response (201 Created)**:

```json
{
  "success": true,
  "data": {
    "created": 8,
    "programs": [{ "id": "prog-1", "name": "Computer Science" }]
  }
}
```

---

#### GET /api/semesters

**Purpose**: Get all semesters

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    { "id": "sem-1", "value": "1" },
    { "id": "sem-2", "value": "2" },
    { "id": "sem-3", "value": "3" },
    { "id": "sem-4", "value": "4" },
    { "id": "sem-5", "value": "5" },
    { "id": "sem-6", "value": "6" },
    { "id": "sem-7", "value": "7" },
    { "id": "sem-8", "value": "8" }
  ]
}
```

---

### User Management Endpoints (Admin Only)

#### GET /api/users

**Purpose**: Get all users with filtering and pagination

**Authorization**: Admin only

**Query Parameters**:

- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `role` (string, optional: "Admin" | "Student")
- `status` (string, optional: "Active" | "Invited" | "Inactive")
- `departmentId` (guid, optional)
- `search` (string, optional - searches name and email)

**Example Request**:

```
GET /api/users?page=1&pageSize=20&role=Student&search=ali
```

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "users": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "email": "ali@kfueit.edu.pk",
        "firstName": "Ali",
        "lastName": "Hassan",
        "role": "Student",
        "status": "Active",
        "department": {
          "id": "abc123",
          "name": "Computer Science"
        },
        "program": "Computer Science",
        "semester": "6",
        "createdAt": "2024-01-15T10:30:00Z"
      }
    ],
    "stats": {
      "statusCounts": {
        "Active": 350,
        "Invited": 80,
        "Inactive": 20
      },
      "roleCounts": {
        "Admin": 5,
        "Student": 445
      }
    },
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalPages": 23,
      "totalCount": 450
    }
  }
}
```

---

#### POST /api/users/import

**Purpose**: Import multiple users from CSV file

**Authorization**: Admin only

**Request (multipart/form-data)**:

```
file: users.csv
```

**CSV Format** (Required Headers):

```csv
email,firstName,lastName,department,program,semester
ahmed.khan@kfueit.edu.pk,Ahmed,Khan,Computer Science,Computer Science,5
fatima.ali@kfueit.edu.pk,Fatima,Ali,Electrical Engineering,Electrical Engineering,3
```

**CSV Validation Rules**:

- **email**: Required, must be valid email, must end with `@kfueit.edu.pk`
- **firstName**: Required, 1-50 characters
- **lastName**: Required, 1-50 characters
- **department**: Required, must match existing department name (case-insensitive)
- **program**: Required, must match existing program name (case-insensitive)
- **semester**: Required, must be valid (1-8)

**Response (200 OK)** - Detailed import result:

```json
{
  "success": true,
  "totalRows": 155,
  "validRows": 150,
  "invalidRows": 5,
  "successfulImports": 148,
  "failedImports": 2,
  "errors": [
    {
      "row": 23,
      "email": "invalid-email",
      "error": "Email must be a KFUEIT email address (@kfueit.edu.pk)"
    },
    {
      "row": 45,
      "email": "student@kfueit.edu.pk",
      "error": "Email already exists"
    },
    {
      "row": 78,
      "email": "test@kfueit.edu.pk",
      "error": "Department 'Physics' not found"
    },
    {
      "row": 102,
      "email": "user@kfueit.edu.pk",
      "error": "Program 'Data Science' not found"
    },
    {
      "row": 130,
      "email": "another@kfueit.edu.pk",
      "error": "Semester '9' not found. Valid semesters: 1-8"
    }
  ],
  "imported": [
    {
      "email": "ahmed.khan@kfueit.edu.pk",
      "firstName": "Ahmed",
      "lastName": "Khan",
      "department": "Computer Science",
      "program": "Computer Science",
      "semester": "5"
    },
    {
      "email": "fatima.ali@kfueit.edu.pk",
      "firstName": "Fatima",
      "lastName": "Ali",
      "department": "Electrical Engineering",
      "program": "Electrical Engineering",
      "semester": "3"
    }
  ]
}
```

**Response (400 Bad Request)** - CSV parsing error:

```json
{
  "success": false,
  "error": "CSV parsing failed",
  "details": [
    {
      "type": "FieldMismatch",
      "code": "TooFewFields",
      "message": "Too few fields: expected 6 fields but got 4",
      "row": 10
    }
  ]
}
```

**Implementation Notes**:

- Use a CSV parsing library (e.g., CsvHelper for .NET)
- Parse with case-insensitive headers
- Transform headers automatically (e.g., `firstname` → `FirstName`)
- Validate each row individually
- Continue processing even if some rows fail
- Return detailed error report with row numbers
- Create users with temporary passwords (to be set on first login)
- Set user status to "Invited" initially
- Duplicate emails are rejected (skip row, add to errors)
- Department/Program/Semester validation is case-sensitive
- Transaction per user (don't rollback all if one fails)

---

### Inquiry Endpoints

#### GET /api/inquiries

**Purpose**: Get all inquiries (Admin: all, Student: active only)

**Authorization**: Required

**Query Parameters** (Admin only):

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `status` (string, optional: "Draft" | "Active" | "Closed")

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "inq-123",
        "body": "How do you feel about the quality of lab equipment available for your courses? Please share specific examples of equipment that works well or needs improvement.",
        "status": "Active",
        "createdBy": {
          "id": "admin-1",
          "firstName": "Dr. Ahmed",
          "lastName": "Khan"
        },
        "targetDepartments": [
          {
            "id": "dept-1",
            "name": "Computer Science"
          },
          {
            "id": "dept-2",
            "name": "Electrical Engineering"
          }
        ],
        "stats": {
          "totalResponses": 245,
          "averageQuality": 0.73,
          "sentimentBreakdown": {
            "positive": 45,
            "neutral": 120,
            "negative": 80
          }
        },
        "hasResponded": false, // For students
        "createdAt": "2024-10-15T10:00:00Z",
        "sentAt": "2024-10-15T14:00:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalPages": 3,
      "totalCount": 52
    }
  }
}
```

---

#### GET /api/inquiries/{id}

**Purpose**: Get inquiry details with inputs

**Authorization**: Required

**Query Parameters**:

- `inputsPage` (int, default: 1)
- `inputsPageSize` (int, default: 20)
- `sentiment` (string, optional filter)
- `minQuality` (double, optional 0.0-1.0)

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "inq-123",
    "body": "How do you feel about the quality of lab equipment...",
    "status": "Active",
    "createdBy": {
      "id": "admin-1",
      "firstName": "Dr. Ahmed",
      "lastName": "Khan"
    },
    "targetDepartments": [
      {
        "id": "dept-1",
        "name": "Computer Science"
      }
    ],
    "aiSummary": {
      "topics": [
        "outdated lab hardware",
        "insufficient RAM capacity",
        "slow compilation times"
      ],
      "executiveSummary": {
        "Headline Insight": "Critical infrastructure gaps in CS lab affecting 80% of students",
        "Response Mix": "245 responses: 80 negative, 120 neutral, 45 positive",
        "Key Takeaways": "Students report 10-15 minute compilation times due to inadequate RAM (8GB) on 85% of lab machines...",
        "Risks": "Continued use of outdated equipment may lead to decreased learning outcomes and student frustration...",
        "Opportunities": "Upgrading lab infrastructure presents opportunity to improve student satisfaction and learning efficiency..."
      },
      "suggestedActions": [
        {
          "action": "Replace lab computers with minimum 16GB RAM and modern processors",
          "impact": "HIGH",
          "challenges": "Budget allocation required (~$50k for 30 machines)",
          "responseCount": 180,
          "supportingReasoning": "Vast majority cite inadequate RAM as primary bottleneck"
        },
        {
          "action": "Install latest IDE versions and development tools",
          "impact": "MEDIUM",
          "challenges": "Minimal - software updates",
          "responseCount": 95,
          "supportingReasoning": "Many students report outdated software versions causing compatibility issues"
        }
      ],
      "generatedAt": "2024-10-20T15:30:00Z"
    },
    "inputs": {
      "items": [
        {
          "id": "input-456",
          "body": "The lab computers are extremely slow. When I compile my Java projects, it takes 10-15 minutes even for small programs. The RAM is only 8GB which is not enough for running IDE and compiler simultaneously.",
          "sentiment": "Negative",
          "tone": "Negative",
          "qualityMetrics": {
            "urgency": 0.85,
            "importance": 0.9,
            "clarity": 0.95,
            "quality": 0.88,
            "helpfulness": 0.92,
            "score": 0.9,
            "severity": 3
          },
          "isAnonymous": true,
          "adminReply": null,
          "createdAt": "2024-10-16T09:20:00Z"
        }
      ],
      "pagination": {
        "currentPage": 1,
        "pageSize": 20,
        "totalPages": 13,
        "totalCount": 245
      }
    },
    "stats": {
      "totalResponses": 245,
      "averageQuality": 0.73,
      "sentimentBreakdown": {
        "positive": 45,
        "neutral": 120,
        "negative": 80
      },
      "severityBreakdown": {
        "high": 85,
        "medium": 120,
        "low": 40
      }
    },
    "createdAt": "2024-10-15T10:00:00Z",
    "sentAt": "2024-10-15T14:00:00Z"
  }
}

// Modify: I think it would be better to have a separate endpoint for inputs in general and we can pass parameters like inquiryId, topicId to distinguish between different input types. This will help in reusing the same endpoint for fetching inputs and avoid bloating the inquiry details response. What do you think? We can add other things like more query params, sorting, pagination etc for inputs
```

---

#### POST /api/inquiries

**Purpose**: Create new inquiry

**Authorization**: Admin only

**Request Body**:

```json
{
  "body": "How satisfied are you with the university's student support services? Please provide specific feedback about counseling, career guidance, or academic advising.",
  "departmentIds": ["dept-1", "dept-2", "dept-3"],
  "status": "Draft"
}
// Modify: We discussed above that each inquiry can have futher tags like programs and semesters so handle it.
```

**Response (201 Created)**:

```json
{
  "success": true,
  "data": {
    "id": "inq-789",
    "body": "How satisfied are you with...",
    "status": "Draft",
    "createdBy": {
      "id": "admin-1",
      "firstName": "Dr. Ahmed",
      "lastName": "Khan"
    },
    "targetDepartments": [
      {
        "id": "dept-1",
        "name": "Computer Science"
      }
    ],
    "createdAt": "2024-11-04T10:30:00Z"
  }
}
```

**Validation Rules**:

- body: Required, minimum 20 characters, maximum 2000 characters
- departmentIds: Required, at least 1 department
- status: Optional, defaults to "Draft"

---

#### PUT /api/inquiries/{id}

**Purpose**: Update inquiry

**Authorization**: Admin only

**Request Body**:

```json
{
  "body": "Updated question text...",
  "departmentIds": ["dept-1", "dept-2"],
  "status": "Active"
}
```

**Response (200 OK)**: Same as POST response

---

#### POST /api/inquiries/{id}/send

**Purpose**: Send inquiry to students (changes status to Active)

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "message": "Inquiry sent to 450 students across 3 departments",
  "data": {
    "id": "inq-123",
    "status": "Active",
    "sentAt": "2024-11-04T10:35:00Z"
  }
}
```

---

#### POST /api/inquiries/{id}/close

**Purpose**: Close inquiry (no more responses accepted)

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "inq-123",
    "status": "Closed",
    "closedAt": "2024-11-04T10:40:00Z"
  }
}
```

---

### Input Endpoints

#### POST /api/inputs

**Purpose**: Submit new input (general or inquiry response)

**Authorization**: Required (Student or Admin)

**Request Body (General Input)**:

```json
{
  "body": "The WiFi connection in the library keeps dropping every 10 minutes. This makes it very difficult to attend online classes or submit assignments on time.",
  "type": "General"
}
```

**Request Body (Inquiry Response)**:

```json
{
  "body": "The lab equipment is outdated and slow...",
  "type": "InquiryLinked",
  "inquiryId": "inq-123"
}
```

**Response (201 Created)**:

```json
{
  "success": true,
  "data": {
    "id": "input-789",
    "body": "The WiFi connection in the library...",
    "type": "General",
    "status": "Pending",
    "isAnonymous": true,
    "createdAt": "2024-11-04T10:45:00Z"
  },
  "message": "Your input has been submitted and will be processed shortly"
}
```

**Validation Rules**:

- body: Required, minimum 20 characters, maximum 2000 characters
- type: Required, must be "General" or "InquiryLinked"
- inquiryId: Required if type is "InquiryLinked"

**Business Rules**:

- Students can only respond once to each inquiry
- Inquiries must be in "Active" status to accept responses
- Input is automatically queued for AI processing

---

#### GET /api/inputs/my-history

**Purpose**: Get current user's input history

**Authorization**: Required (Student)

**Query Parameters**:

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `type` (string, optional filter)

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "input-789",
        "body": "The WiFi connection in the library...",
        "type": "General",
        "status": "Processed",
        "theme": {
          "id": "theme-1",
          "name": "Infrastructure"
        },
        "topic": {
          "id": "topic-5",
          "name": "Internet Connectivity Issues"
        },
        "qualityMetrics": {
          "urgency": 0.75,
          "importance": 0.8,
          "clarity": 0.9,
          "quality": 0.82,
          "helpfulness": 0.85,
          "score": 0.82,
          "severity": 3
        },
        "adminReply": null,
        "revealRequested": false,
        "createdAt": "2024-11-04T10:45:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalPages": 2,
      "totalCount": 35
    }
  }
}
```

---

#### GET /api/inputs/{id}

**Purpose**: Get input details

**Authorization**: Required (Admin: any input, Student: own inputs only)

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "input-789",
    "body": "The WiFi connection in the library keeps dropping...",
    "type": "General",
    "status": "Processed",
    "user": {
      "id": "user-123",
      "firstName": "Anonymous",
      "lastName": "Student",
      "isRevealed": false
    },
    "inquiry": null,
    "theme": {
      "id": "theme-1",
      "name": "Infrastructure"
    },
    "topic": {
      "id": "topic-5",
      "name": "Internet Connectivity Issues"
    },
    "sentiment": "Negative",
    "tone": "Negative",
    "qualityMetrics": {
      "urgency": 0.75,
      "importance": 0.8,
      "clarity": 0.9,
      "quality": 0.82,
      "helpfulness": 0.85,
      "score": 0.82,
      "severity": 3
    },
    // "adminReply": {
    //   "message": "Thank you for reporting this issue. Our IT team is investigating the WiFi connectivity problems in the library...",
    //   "repliedAt": "2024-11-04T15:20:00Z",
    //   "repliedBy": {
    //     "id": "admin-1",
    //     "firstName": "Dr. Ahmed",
    //     "lastName": "Khan"
    //   }
    // },
    // Modify: We discussed above that InputReply is gonna be a separate entity so it needs to be handled separately.
    "revealRequested": true,
    "revealApproved": null,
    "createdAt": "2024-11-04T10:45:00Z",
    "updatedAt": "2024-11-04T15:20:00Z"
  }
}
```

---

<!-- #### POST /api/inputs/{id}/reply

**Purpose**: Admin replies to an input

**Authorization**: Admin only

**Request Body**:

```json
{
  "reply": "Thank you for your detailed feedback. We have forwarded your concerns about the lab equipment to the IT department. They will be conducting an assessment next week and exploring budget options for upgrades."
}
```

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "input-789",
    "adminReply": {
      "message": "Thank you for your detailed feedback...",
      "repliedAt": "2024-11-04T15:25:00Z",
      "repliedBy": {
        "id": "admin-1",
        "firstName": "Dr. Ahmed",
        "lastName": "Khan"
      }
    }
  }
}
```

**Validation Rules**:

- reply: Required, minimum 10 characters, maximum 1000 characters -->

<!--Modify: As we are gonna handle admin replies separately as a separate entity so we need to do things accordingly. -->

---

#### POST /api/inputs/{id}/request-reveal

**Purpose**: Admin requests identity reveal

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "message": "Identity reveal request sent to the student",
  "data": {
    "id": "input-789",
    "revealRequested": true
  }
}
```

---

#### POST /api/inputs/{id}/respond-reveal

**Purpose**: Student approves/denies identity reveal

**Authorization**: Student (must be input owner)

**Request Body**:

```json
{
  "approved": true
}
```

**Response (200 OK)**:

```json
{
  "success": true,
  "message": "Identity reveal response recorded",
  "data": {
    "id": "input-789",
    "revealApproved": true
  }
}
```

---

### Topic Endpoints

#### GET /api/topics

**Purpose**: Get all topics with stats

**Authorization**: Admin only

**Query Parameters**:

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `departmentId` (guid, optional filter)
- `sortBy` (string: "name" | "inputCount" | "createdAt", default: "inputCount")
- `sortOrder` (string: "asc" | "desc", default: "desc")

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "topic-5",
        "name": "Internet Connectivity Issues",
        "department": {
          "id": "dept-1",
          "name": "Computer Science"
        },
        "stats": {
          "totalInputs": 45,
          "averageQuality": 0.78,
          "sentimentBreakdown": {
            "positive": 5,
            "neutral": 15,
            "negative": 25
          },
          "severityBreakdown": {
            "high": 30,
            "medium": 10,
            "low": 5
          }
        },
        "hasSummary": true,
        "createdAt": "2024-10-01T08:00:00Z",
        "updatedAt": "2024-11-04T10:45:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalPages": 4,
      "totalCount": 78
    }
  }
}
```

---

#### GET /api/topics/{id}

**Purpose**: Get topic details with AI summary

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "id": "topic-5",
    "name": "Internet Connectivity Issues",
    "department": {
      "id": "dept-1",
      "name": "Computer Science"
    },
    "aiSummary": {
      "topics": [
        "library wifi disconnections",
        "slow network speeds",
        "VPN access issues"
      ],
      "executiveSummary": {
        "Headline Insight": "Persistent WiFi connectivity problems affecting 45 students across campus",
        "Response Mix": "45 inputs: 25 negative, 15 neutral, 5 positive",
        "Key Takeaways": "Students report frequent disconnections (every 5-10 minutes) particularly in library and hostels...",
        "Risks": "Continued connectivity issues may impact online class attendance and assignment submissions...",
        "Opportunities": "Infrastructure upgrade could significantly improve student satisfaction and learning experience..."
      },
      "suggestedActions": [
        {
          "action": "Upgrade WiFi routers in library and hostels",
          "impact": "HIGH",
          "challenges": "Equipment cost (~$15k) and installation downtime",
          "responseCount": 35,
          "supportingReasoning": "Majority of complaints from these two locations"
        }
      ],
      "generatedAt": "2024-11-04T12:00:00Z"
    },
    // "inputs": {
    //   "items": [
    //     {
    //       "id": "input-789",
    //       "body": "The WiFi connection in the library keeps dropping...",
    //       "sentiment": "Negative",
    //       "tone": "Negative",
    //       "qualityMetrics": {
    //         "urgency": 0.75,
    //         "importance": 0.8,
    //         "clarity": 0.9,
    //         "quality": 0.82,
    //         "helpfulness": 0.85,
    //         "score": 0.82,
    //         "severity": 3
    //       },
    //       "isAnonymous": true,
    //       "adminReply": null,
    //       "createdAt": "2024-11-04T10:45:00Z"
    //     }
    //   ],
    //   "pagination": {
    //     "currentPage": 1,
    //     "pageSize": 20,
    //     "totalPages": 3,
    //     "totalCount": 45
    //   }
    // },
    // Modify: We discussed above that Inputs will be handled separately so we can skip it here.
    "stats": {
      "totalInputs": 45,
      "averageQuality": 0.78,
      "sentimentBreakdown": {
        "positive": 5,
        "neutral": 15,
        "negative": 25
      },
      "severityBreakdown": {
        "high": 30,
        "medium": 10,
        "low": 5
      }
    },
    "createdAt": "2024-10-01T08:00:00Z",
    "updatedAt": "2024-11-04T10:45:00Z"
  }
}
```

---

### Dashboard Endpoints

#### GET /api/dashboard/stats

**Purpose**: Get dashboard overview statistics

**Authorization**: Admin only

**Response (200 OK)**:

```json
{
  "success": true,
  "data": {
    "overview": {
      "totalInputs": 1247,
      "totalInquiries": 15,
      "activeInquiries": 5,
      "totalTopics": 78,
      "totalStudents": 450
    },
    "recentActivity": {
      "inputsLastWeek": 87,
      "inputsThisWeek": 45,
      "percentageChange": -48.3
    },
    "sentimentDistribution": {
      "positive": 234,
      "neutral": 678,
      "negative": 335
    },
    "topTopics": [
      {
        "id": "topic-5",
        "name": "Internet Connectivity Issues",
        "inputCount": 45,
        "averageQuality": 0.78,
        "trend": "increasing"
      },
      {
        "id": "topic-12",
        "name": "Lab Equipment Quality",
        "inputCount": 38,
        "averageQuality": 0.82,
        "trend": "stable"
      }
    ],
    "urgentInputs": [
      {
        "id": "input-999",
        "body": "Safety hazard in electrical lab...",
        "severity": 3,
        "score": 0.95,
        "createdAt": "2024-11-04T09:00:00Z"
      }
    ],
    "departmentBreakdown": [
      {
        "departmentId": "dept-1",
        "departmentName": "Computer Science",
        "inputCount": 345,
        "averageQuality": 0.75
      },
      {
        "departmentId": "dept-2",
        "departmentName": "Electrical Engineering",
        "inputCount": 278,
        "averageQuality": 0.79
      }
    ]
  }
}
```

---

### Department/Program/Semester Endpoints

#### GET /api/departments

**Purpose**: Get all departments

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    {
      "id": "dept-1",
      "name": "Computer Science",
      "description": "Computer Science and Software Engineering"
    },
    {
      "id": "dept-2",
      "name": "Electrical Engineering",
      "description": "Electrical and Electronics Engineering"
    }
  ]
}
```

#### GET /api/programs

**Purpose**: Get all programs

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    {
      "id": "prog-1",
      "name": "BS Computer Science"
    },
    {
      "id": "prog-2",
      "name": "BS Software Engineering"
    }
  ]
}
```

#### GET /api/semesters

**Purpose**: Get all semesters

**Response (200 OK)**:

```json
{
  "success": true,
  "data": [
    { "id": "sem-1", "value": "1" },
    { "id": "sem-2", "value": "2" },
    { "id": "sem-3", "value": "3" },
    { "id": "sem-4", "value": "4" },
    { "id": "sem-5", "value": "5" },
    { "id": "sem-6", "value": "6" },
    { "id": "sem-7", "value": "7" },
    { "id": "sem-8", "value": "8" }
  ]
}
```

---

## Service Layer

### IAiProcessingService Interface

```csharp
// SmartInsights.Application/Interfaces/IAiProcessingService.cs
namespace SmartInsights.Application.Interfaces;

public interface IAiProcessingService
{
    // Stage 1: Classification (for General Inputs)
    Task<ClassificationResult> ClassifyGeneralInputAsync(Input input);

    // Stage 2: Quality Scoring (for all Inputs)
    Task<QualityScoresResult> CalculateQualityScoresAsync(Input input);

    // Stage 3: Topic Matching (for General Inputs)
    Task<TopicMatchResult> FindOrCreateTopicAsync(Input input, List<Topic> existingTopics);

    // Aggregate Analysis
    Task<ExecutiveSummary> GenerateInquirySummaryAsync(Inquiry inquiry, List<Input> inputs);
    Task<ExecutiveSummary> GenerateTopicSummaryAsync(Topic topic, List<Input> inputs);
}

// Result DTOs
public class ClassificationResult
{
    public string Theme { get; set; } = string.Empty;
    public string SuggestedTopic { get; set; } = string.Empty;
    public List<string> Departments { get; set; } = new();
    public Sentiment Sentiment { get; set; }
    public Tone Tone { get; set; }
}

public class QualityScoresResult
{
    public double UrgencyPct { get; set; }
    public double ImportancePct { get; set; }
    public double ClarityPct { get; set; }
    public double QualityPct { get; set; }
    public double HelpfulnessPct { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class TopicMatchResult
{
    public Guid? ExistingTopicId { get; set; }
    public string? NewTopicName { get; set; }
    public double MatchScore { get; set; }
}
```

### AzureOpenAiService Implementation

```csharp
// SmartInsights.Infrastructure/AI/AzureOpenAiService.cs
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using SmartInsights.Application.Interfaces;
using System.Text.Json;

namespace SmartInsights.Infrastructure.AI;

public class AzureOpenAiService : IAiProcessingService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly IConfiguration _configuration;

    public AzureOpenAiService(IConfiguration configuration)
    {
        _configuration = configuration;
        var endpoint = new Uri(configuration["AzureOpenAI:Endpoint"]!);
        var apiKey = configuration["AzureOpenAI:ApiKey"]!;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"]!;

        _client = new OpenAIClient(endpoint, new AzureKeyCredential(apiKey));
    }

    public async Task<ClassificationResult> ClassifyGeneralInputAsync(Input input)
    {
        var systemPrompt = PromptTemplates.GetClassificationPrompt();
        var userPrompt = $"Analyze this student feedback:\n\n{input.Body}";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.3f,
            MaxTokens = 500,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var response = await _client.GetChatCompletionsAsync(options);
        var content = response.Value.Choices[0].Message.Content;

        return JsonSerializer.Deserialize<ClassificationResult>(content)!;
    }

    public async Task<QualityScoresResult> CalculateQualityScoresAsync(Input input)
    {
        var systemPrompt = PromptTemplates.GetQualityScoringPrompt();
        var userPrompt = $"Analyze the quality of this feedback:\n\n{input.Body}";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.2f,
            MaxTokens = 400,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var response = await _client.GetChatCompletionsAsync(options);
        var content = response.Value.Choices[0].Message.Content;

        return JsonSerializer.Deserialize<QualityScoresResult>(content)!;
    }

    public async Task<TopicMatchResult> FindOrCreateTopicAsync(Input input, List<Topic> existingTopics)
    {
        if (!existingTopics.Any())
        {
            // Create first topic
            var newTopicName = await GenerateTopicNameAsync(input.Body);
            return new TopicMatchResult
            {
                NewTopicName = newTopicName,
                MatchScore = 1.0
            };
        }

        var systemPrompt = PromptTemplates.GetTopicMatchingPrompt();
        var topicsJson = JsonSerializer.Serialize(existingTopics.Select(t => new { t.Id, t.Name }));
        var userPrompt = $"Existing topics:\n{topicsJson}\n\nNew feedback:\n{input.Body}\n\nFind best matching topic or suggest creating new one.";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.3f,
            MaxTokens = 200,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var response = await _client.GetChatCompletionsAsync(options);
        var content = response.Value.Choices[0].Message.Content;

        return JsonSerializer.Deserialize<TopicMatchResult>(content)!;
    }

    public async Task<ExecutiveSummary> GenerateInquirySummaryAsync(Inquiry inquiry, List<Input> inputs)
    {
        var systemPrompt = PromptTemplates.GetExecutiveSummaryPrompt();
        var inputsText = string.Join("\n\n", inputs.Select((inp, idx) =>
            $"Input {idx + 1}:\n{inp.Body}\nSentiment: {inp.Sentiment}\nQuality: {inp.Score:P0}"
        ));

        var userPrompt = $"Inquiry Question:\n{inquiry.Body}\n\nTotal Responses: {inputs.Count}\n\nSample Responses:\n{inputsText.Substring(0, Math.Min(inputsText.Length, 8000))}";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.4f,
            MaxTokens = 1500,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var response = await _client.GetChatCompletionsAsync(options);
        var content = response.Value.Choices[0].Message.Content;

        return JsonSerializer.Deserialize<ExecutiveSummary>(content)!;
    }

    public async Task<ExecutiveSummary> GenerateTopicSummaryAsync(Topic topic, List<Input> inputs)
    {
        // Similar to GenerateInquirySummaryAsync but context is topic instead of inquiry
        var systemPrompt = PromptTemplates.GetExecutiveSummaryPrompt();
        var inputsText = string.Join("\n\n", inputs.Select((inp, idx) =>
            $"Input {idx + 1}:\n{inp.Body}\nSentiment: {inp.Sentiment}\nQuality: {inp.Score:P0}"
        ));

        var userPrompt = $"Topic: {topic.Name}\n\nTotal Inputs: {inputs.Count}\n\nSample Inputs:\n{inputsText.Substring(0, Math.Min(inputsText.Length, 8000))}";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.4f,
            MaxTokens = 1500,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var response = await _client.GetChatCompletionsAsync(options);
        var content = response.Value.Choices[0].Message.Content;

        return JsonSerializer.Deserialize<ExecutiveSummary>(content)!;
    }

    private async Task<string> GenerateTopicNameAsync(string inputBody)
    {
        var systemPrompt = "You are an expert at creating concise, descriptive topic names. Generate a topic name (maximum 5 words) that captures the main issue or theme.";
        var userPrompt = $"Generate a topic name for this feedback:\n\n{inputBody}";

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.5f,
            MaxTokens = 20
        };

        var response = await _client.GetChatCompletionsAsync(options);
        return response.Value.Choices[0].Message.Content.Trim().Trim('"');
    }
}
```

### Prompt Templates

```csharp
// SmartInsights.Infrastructure/AI/Prompts/PromptTemplates.cs
namespace SmartInsights.Infrastructure.AI.Prompts;

public static class PromptTemplates
{
    public static string GetClassificationPrompt()
    {
        return @"You are analyzing student feedback for KFUEIT University.

TASK: Classify the feedback into theme, suggest a topic name, identify relevant departments, and analyze sentiment/tone.

THEMES (choose one):
- Infrastructure (facilities, equipment, buildings)
- Academic (curriculum, teaching, exams)
- Administration (policies, procedures, services)
- Student Life (events, activities, support)
- Technology (software, systems, digital tools)
- Other

DEPARTMENTS (select all relevant):
- Computer Science
- Electrical Engineering
- Mechanical Engineering
- Civil Engineering
- Management Sciences
- General (if not department-specific)

SENTIMENT: Positive, Neutral, or Negative
TONE: Positive, Neutral, or Negative

OUTPUT FORMAT (JSON):
{
  ""theme"": ""Infrastructure"",
  ""suggestedTopic"": ""Lab Equipment Quality"",
  ""departments"": [""Computer Science"", ""Electrical Engineering""],
  ""sentiment"": ""Negative"",
  ""tone"": ""Negative""
}";
    }

    public static string GetQualityScoringPrompt()
    {
        return @"You are evaluating the quality of student feedback for university administration.

TASK: Rate the feedback on 5 dimensions (0.0 to 1.0 scale).

DIMENSIONS:
1. URGENCY: How time-sensitive is the issue?
   - 0.9-1.0: Immediate safety/critical issue
   - 0.7-0.8: Important, needs attention soon
   - 0.4-0.6: Moderate priority
   - 0.0-0.3: Low urgency, can be addressed later

2. IMPORTANCE: How significant is the impact?
   - 0.9-1.0: Affects many students, critical to learning
   - 0.7-0.8: Significant impact on student experience
   - 0.4-0.6: Moderate impact
   - 0.0-0.3: Minor inconvenience

3. CLARITY: How clear and specific is the feedback?
   - 0.9-1.0: Very specific with examples/details
   - 0.7-0.8: Clear and understandable
   - 0.4-0.6: Somewhat vague
   - 0.0-0.3: Very unclear or confusing

4. QUALITY: Overall quality of the feedback?
   - 0.9-1.0: Excellent, actionable, constructive
   - 0.7-0.8: Good, useful information
   - 0.4-0.6: Average
   - 0.0-0.3: Poor quality, not helpful

5. HELPFULNESS: How actionable is this feedback?
   - 0.9-1.0: Very actionable, clear next steps
   - 0.7-0.8: Actionable with some interpretation
   - 0.4-0.6: Somewhat actionable
   - 0.0-0.3: Not actionable

OUTPUT FORMAT (JSON):
{
  ""urgencyPct"": 0.85,
  ""importancePct"": 0.90,
  ""clarityPct"": 0.95,
  ""qualityPct"": 0.88,
  ""helpfulnessPct"": 0.92,
  ""explanation"": ""Brief explanation of scores""
}";
    }

    public static string GetTopicMatchingPrompt()
    {
        return @"You are matching student feedback to existing topics.

TASK: Find the best matching topic OR suggest creating a new one.

MATCHING CRITERIA:
- Topics should group similar issues/themes
- Match score 0.7+ means good match
- Match score below 0.7 suggests creating new topic
- New topic names should be concise (max 5 words)

OUTPUT FORMAT (JSON):

For existing topic match:
{
  ""existingTopicId"": ""guid-here"",
  ""newTopicName"": null,
  ""matchScore"": 0.85
}

For new topic creation:
{
  ""existingTopicId"": null,
  ""newTopicName"": ""WiFi Connectivity Problems"",
  ""matchScore"": 1.0
}";
    }

    public static string GetExecutiveSummaryPrompt()
    {
        return @"You are creating an executive summary of student feedback for university administration.

TASK: Analyze all responses and create a structured executive summary.

OUTPUT FORMAT (JSON):
{
  ""topics"": [""topic 1"", ""topic 2"", ""topic 3""],
  ""executiveSummary"": {
    ""Headline Insight"": ""One sentence capturing the main finding"",
    ""Response Mix"": ""X responses: Y negative, Z neutral, W positive"",
    ""Key Takeaways"": ""2-3 paragraphs with main themes, specific examples, and patterns"",
    ""Risks"": ""Potential negative consequences if not addressed"",
    ""Opportunities"": ""Positive outcomes possible by addressing feedback""
  },
  ""suggestedPrioritizedActions"": [
    {
      ""action"": ""Specific action to take"",
      ""impact"": ""HIGH/MEDIUM/LOW"",
      ""challenges"": ""Implementation challenges"",
      ""responseCount"": 150,
      ""supportingReasoning"": ""Why this action is recommended""
    }
  ]
}

GUIDELINES:
- Be specific and data-driven
- Quote actual student feedback when relevant
- Prioritize actions by impact and feasibility
- Keep tone professional and constructive";
    }
}
```

### CSV Import Service Implementation

```csharp
// SmartInsights.Application/Services/CsvImportService.cs
using CsvHelper;
using CsvHelper.Configuration;
using SmartInsights.Application.DTOs.Users;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartInsights.Application.Services;

public class CsvImportService : ICsvImportService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<Program> _programRepository;
    private readonly IRepository<Semester> _semesterRepository;

    public CsvImportService(
        IRepository<User> userRepository,
        IRepository<Department> departmentRepository,
        IRepository<Program> programRepository,
        IRepository<Semester> semesterRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _programRepository = programRepository;
        _semesterRepository = semesterRepository;
    }

    public async Task<BulkImportResult> ImportUsersFromCsvAsync(Stream csvStream)
    {
        var result = new BulkImportResult
        {
            Success = false,
            TotalRows = 0,
            ValidRows = 0,
            InvalidRows = 0,
            SuccessfulImports = 0,
            FailedImports = 0,
            Errors = new List<ImportError>(),
            Imported = new List<ImportedUser>()
        };

        // Load reference data
        var departments = await _departmentRepository.GetAllAsync();
        var programs = await _programRepository.GetAllAsync();
        var semesters = await _semesterRepository.GetAllAsync();

        // Create lookup dictionaries (case-insensitive)
        var departmentMap = departments.ToDictionary(d => d.Name.ToLower(), d => d);
        var programMap = programs.ToDictionary(p => p.Name.ToLower(), p => p);
        var semesterMap = semesters.ToDictionary(s => s.Value, s => s);

        using var reader = new StreamReader(csvStream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.ToLower().Trim()
        };

        using var csv = new CsvReader(reader, config);

        // Read all records
        List<CsvUserRecord> records;
        try
        {
            records = csv.GetRecords<CsvUserRecord>().ToList();
            result.TotalRows = records.Count;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError
            {
                Row = 0,
                Email = "N/A",
                Error = $"CSV parsing error: {ex.Message}"
            });
            return result;
        }

        // Process each row
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var rowNumber = i + 2; // +2 for header and 0-index

            try
            {
                // Validate email
                if (string.IsNullOrWhiteSpace(record.Email))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email ?? "empty",
                        Error = "Email is required"
                    });
                    continue;
                }

                // Validate KFUEIT email domain
                if (!record.Email.ToLower().EndsWith("@kfueit.edu.pk"))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Email must be a KFUEIT email address (@kfueit.edu.pk)"
                    });
                    continue;
                }

                // Validate email format
                if (!IsValidEmail(record.Email))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Invalid email format"
                    });
                    continue;
                }

                // Validate first name
                if (string.IsNullOrWhiteSpace(record.FirstName))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "First name is required"
                    });
                    continue;
                }

                // Validate last name
                if (string.IsNullOrWhiteSpace(record.LastName))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Last name is required"
                    });
                    continue;
                }

                // Validate department
                if (string.IsNullOrWhiteSpace(record.Department))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Department is required"
                    });
                    continue;
                }

                var departmentKey = record.Department.ToLower().Trim();
                if (!departmentMap.ContainsKey(departmentKey))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = $"Department '{record.Department}' not found"
                    });
                    continue;
                }

                // Validate program
                if (string.IsNullOrWhiteSpace(record.Program))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Program is required"
                    });
                    continue;
                }

                var programKey = record.Program.ToLower().Trim();
                if (!programMap.ContainsKey(programKey))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = $"Program '{record.Program}' not found"
                    });
                    continue;
                }

                // Validate semester
                if (string.IsNullOrWhiteSpace(record.Semester))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Semester is required"
                    });
                    continue;
                }

                var semesterValue = record.Semester.Trim();
                if (!semesterMap.ContainsKey(semesterValue))
                {
                    result.InvalidRows++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = $"Semester '{record.Semester}' not found. Valid semesters: 1-8"
                    });
                    continue;
                }

                result.ValidRows++;

                // Check if user already exists
                var existingUser = await _userRepository.FirstOrDefaultAsync(
                    u => u.Email.ToLower() == record.Email.ToLower());

                if (existingUser != null)
                {
                    result.FailedImports++;
                    result.Errors.Add(new ImportError
                    {
                        Row = rowNumber,
                        Email = record.Email,
                        Error = "Email already exists"
                    });
                    continue;
                }

                // Create user
                var department = departmentMap[departmentKey];
                var program = programMap[programKey];
                var semester = semesterMap[semesterValue];

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = record.Email.ToLower().Trim(),
                    FirstName = record.FirstName.Trim(),
                    LastName = record.LastName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateTemporaryPassword()),
                    Role = Role.Student,
                    Status = UserStatus.Invited,
                    DepartmentId = department.Id,
                    ProgramId = program.Id,
                    SemesterId = semester.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);

                result.SuccessfulImports++;
                result.Imported.Add(new ImportedUser
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Department = department.Name,
                    Program = program.Name,
                    Semester = semester.Value
                });
            }
            catch (Exception ex)
            {
                result.FailedImports++;
                result.Errors.Add(new ImportError
                {
                    Row = rowNumber,
                    Email = record.Email ?? "unknown",
                    Error = $"Failed to create user: {ex.Message}"
                });
            }
        }

        result.Success = result.SuccessfulImports > 0;
        return result;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private string GenerateTemporaryPassword()
    {
        // Generate random 12-character password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

// CSV Record mapping class
public class CsvUserRecord
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
}

// DTOs for CSV Import
public class BulkImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public List<ImportedUser> Imported { get; set; } = new();
}

public class ImportError
{
    public int Row { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class ImportedUser
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
}
```

### Workspace Setup Service

```csharp
// SmartInsights.Application/Services/WorkspaceService.cs
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<Program> _programRepository;
    private readonly IRepository<Semester> _semesterRepository;
    private readonly IRepository<Theme> _themeRepository;
    private readonly IRepository<User> _userRepository;

    public WorkspaceService(
        IRepository<Department> departmentRepository,
        IRepository<Program> programRepository,
        IRepository<Semester> semesterRepository,
        IRepository<Theme> themeRepository,
        IRepository<User> userRepository)
    {
        _departmentRepository = departmentRepository;
        _programRepository = programRepository;
        _semesterRepository = semesterRepository;
        _themeRepository = themeRepository;
        _userRepository = userRepository;
    }

    public async Task<WorkspaceStatus> GetSetupStatusAsync()
    {
        var departmentCount = await _departmentRepository.CountAsync();
        var programCount = await _programRepository.CountAsync();
        var semesterCount = await _semesterRepository.CountAsync();
        var userCount = await _userRepository.CountAsync();

        return new WorkspaceStatus
        {
            IsSetup = departmentCount > 0 && programCount > 0 && semesterCount > 0,
            DepartmentCount = departmentCount,
            ProgramCount = programCount,
            SemesterCount = semesterCount,
            UserCount = userCount
        };
    }

    public async Task<List<Department>> CreateDepartmentsBulkAsync(List<CreateDepartmentDto> departments)
    {
        var createdDepartments = new List<Department>();

        foreach (var dto in departments)
        {
            // Check if department already exists
            var existing = await _departmentRepository.FirstOrDefaultAsync(
                d => d.Name.ToLower() == dto.Name.ToLower());

            if (existing != null)
                continue;

            var department = new Department
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _departmentRepository.AddAsync(department);
            createdDepartments.Add(department);
        }

        return createdDepartments;
    }

    public async Task<List<Program>> CreateProgramsBulkAsync(List<string> programNames)
    {
        var createdPrograms = new List<Program>();

        foreach (var name in programNames)
        {
            // Check if program already exists
            var existing = await _programRepository.FirstOrDefaultAsync(
                p => p.Name.ToLower() == name.ToLower());

            if (existing != null)
                continue;

            var program = new Program
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            await _programRepository.AddAsync(program);
            createdPrograms.Add(program);
        }

        return createdPrograms;
    }

    public async Task InitializeSemestersAsync()
    {
        // Create semesters 1-8 if they don't exist
        for (int i = 1; i <= 8; i++)
        {
            var semesterValue = i.ToString();
            var existing = await _semesterRepository.FirstOrDefaultAsync(
                s => s.Value == semesterValue);

            if (existing == null)
            {
                var semester = new Semester
                {
                    Id = Guid.NewGuid(),
                    Value = semesterValue,
                    CreatedAt = DateTime.UtcNow
                };

                await _semesterRepository.AddAsync(semester);
            }
        }
    }

    public async Task InitializeThemesAsync()
    {
        var themeNames = new List<string>
        {
            "Infrastructure",
            "Academic",
            "Administration",
            "Student Life",
            "Technology",
            "Other"
        };

        foreach (var themeName in themeNames)
        {
            var existing = await _themeRepository.FirstOrDefaultAsync(
                t => t.Name.ToLower() == themeName.ToLower());

            if (existing == null)
            {
                var theme = new Theme
                {
                    Id = Guid.NewGuid(),
                    Name = themeName,
                    CreatedAt = DateTime.UtcNow
                };

                await _themeRepository.AddAsync(theme);
            }
        }
    }
}

// DTOs
public class WorkspaceStatus
{
    public bool IsSetup { get; set; }
    public int DepartmentCount { get; set; }
    public int ProgramCount { get; set; }
    public int SemesterCount { get; set; }
    public int UserCount { get; set; }
}

public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

### Input Processing Service

```csharp
// SmartInsights.Application/Services/InputService.cs
using SmartInsights.Application.DTOs.Inputs;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Application.Services;

public class InputService : IInputService
{
    private readonly IRepository<Input> _inputRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Theme> _themeRepository;
    private readonly IAiProcessingService _aiService;

    public InputService(
        IRepository<Input> inputRepository,
        IRepository<Topic> topicRepository,
        IRepository<Theme> themeRepository,
        IAiProcessingService aiService)
    {
        _inputRepository = inputRepository;
        _topicRepository = topicRepository;
        _themeRepository = themeRepository;
        _aiService = aiService;
    }

    public async Task<Input> CreateInputAsync(CreateInputDto dto, Guid userId)
    {
        var input = new Input
        {
            Id = Guid.NewGuid(),
            Body = dto.Body,
            Type = dto.Type,
            UserId = userId,
            InquiryId = dto.InquiryId,
            Status = InputStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _inputRepository.AddAsync(input);

        // Queue for AI processing (will be handled by background job)
        return input;
    }

    public async Task ProcessInputAsync(Guid inputId)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null || input.Status != InputStatus.Pending)
            return;

        try
        {
            input.Status = InputStatus.Processing;
            input.UpdatedAt = DateTime.UtcNow;
            await _inputRepository.UpdateAsync(input);

            // Stage 1: Classification (for General inputs only)
            if (input.Type == InputType.General)
            {
                var classification = await _aiService.ClassifyGeneralInputAsync(input);

                input.Sentiment = classification.Sentiment;
                input.Tone = classification.Tone;

                // Find or create theme
                var theme = await _themeRepository.FirstOrDefaultAsync(t => t.Name == classification.Theme);
                if (theme != null)
                {
                    input.ThemeId = theme.Id;
                }

                // Find or create topic
                var existingTopics = await _topicRepository.FindAsync(t => t.Name.Contains(classification.SuggestedTopic));
                var topicMatch = await _aiService.FindOrCreateTopicAsync(input, existingTopics);

                if (topicMatch.ExistingTopicId.HasValue)
                {
                    input.TopicId = topicMatch.ExistingTopicId.Value;
                }
                else if (!string.IsNullOrEmpty(topicMatch.NewTopicName))
                {
                    var newTopic = new Topic
                    {
                        Id = Guid.NewGuid(),
                        Name = topicMatch.NewTopicName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _topicRepository.AddAsync(newTopic);
                    input.TopicId = newTopic.Id;
                }
            }
            else // InquiryLinked
            {
                // For inquiry responses, just extract sentiment/tone
                var classification = await _aiService.ClassifyGeneralInputAsync(input);
                input.Sentiment = classification.Sentiment;
                input.Tone = classification.Tone;
            }

            // Stage 2: Quality Scoring (for all inputs)
            var qualityScores = await _aiService.CalculateQualityScoresAsync(input);
            input.UrgencyPct = qualityScores.UrgencyPct;
            input.ImportancePct = qualityScores.ImportancePct;
            input.ClarityPct = qualityScores.ClarityPct;
            input.QualityPct = qualityScores.QualityPct;
            input.HelpfulnessPct = qualityScores.HelpfulnessPct;
            input.CalculateScore();

            input.Status = InputStatus.Processed;
            input.UpdatedAt = DateTime.UtcNow;
            await _inputRepository.UpdateAsync(input);
        }
        catch (Exception ex)
        {
            input.Status = InputStatus.Error;
            input.UpdatedAt = DateTime.UtcNow;
            await _inputRepository.UpdateAsync(input);
            throw;
        }
    }

    public async Task<Input?> GetInputByIdAsync(Guid id)
    {
        return await _inputRepository.GetByIdWithIncludesAsync(id,
            i => i.User,
            i => i.Inquiry,
            i => i.Topic,
            i => i.Theme);
    }

    public async Task<List<Input>> GetUserInputsAsync(Guid userId, int page, int pageSize)
    {
        return await _inputRepository.GetPagedAsync(
            filter: i => i.UserId == userId,
            orderBy: q => q.OrderByDescending(i => i.CreatedAt),
            page: page,
            pageSize: pageSize);
    }

    // public async Task AddAdminReplyAsync(Guid inputId, string reply, Guid adminId)
    // {
    //     var input = await _inputRepository.GetByIdAsync(inputId);
    //     if (input == null)
    //         throw new Exception("Input not found");

    //     input.AdminReply = reply;
    //     input.RepliedAt = DateTime.UtcNow;
    //     input.UpdatedAt = DateTime.UtcNow;

    //     await _inputRepository.UpdateAsync(input);
    // }
    // Modify: We are handling input replying as a separate entity so .

    public async Task RequestIdentityRevealAsync(Guid inputId)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null)
            throw new Exception("Input not found");

        input.RevealRequested = true;
        input.UpdatedAt = DateTime.UtcNow;

        await _inputRepository.UpdateAsync(input);
    }

    public async Task RespondToRevealRequestAsync(Guid inputId, Guid userId, bool approved)
    {
        var input = await _inputRepository.GetByIdAsync(inputId);
        if (input == null || input.UserId != userId)
            throw new Exception("Input not found or unauthorized");

        if (!input.RevealRequested)
            throw new Exception("No reveal request pending");

        input.RevealApproved = approved;
        input.UpdatedAt = DateTime.UtcNow;

        await _inputRepository.UpdateAsync(input);
    }
}
```

### Background Job Processing

```csharp
// SmartInsights.Infrastructure/BackgroundJobs/InputProcessingJob.cs
using Hangfire;
using SmartInsights.Application.Interfaces;

namespace SmartInsights.Infrastructure.BackgroundJobs;

public class InputProcessingJob
{
    private readonly IInputService _inputService;

    public InputProcessingJob(IInputService inputService)
    {
        _inputService = inputService;
    }

    public async Task ProcessPendingInputs()
    {
        // This job runs every minute to process pending inputs
        var pendingInputs = await _inputService.GetPendingInputsAsync();

        foreach (var input in pendingInputs)
        {
            // Process each input in background
            BackgroundJob.Enqueue(() => _inputService.ProcessInputAsync(input.Id));
        }
    }
}

// Registration in Program.cs
public static class HangfireConfiguration
{
    public static void ConfigureHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection")));

        services.AddHangfireServer();

        // Schedule recurring job
        RecurringJob.AddOrUpdate<InputProcessingJob>(
            "process-pending-inputs",
            job => job.ProcessPendingInputs(),
            Cron.Minutely);
    }
}
```

---

## Authentication & Authorization

### JWT Configuration

```csharp
// SmartInsights.API/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});
```

### Auth Service Implementation

```csharp
// SmartInsights.Application/Services/AuthService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartInsights.Application.DTOs.Auth;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace SmartInsights.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<User> userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.Email == dto.Email,
            include: q => q.Include(u => u.Department)
                          .Include(u => u.Program)
                          .Include(u => u.Semester));

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("User account is inactive");
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        if (user.DepartmentId.HasValue)
            claims.Add(new Claim("departmentId", user.DepartmentId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            Department = user.Department != null ? new DepartmentDto
            {
                Id = user.Department.Id,
                Name = user.Department.Name
            } : null,
            Program = user.Program != null ? new ProgramDto
            {
                Id = user.Program.Id,
                Name = user.Program.Name
            } : null,
            Semester = user.Semester != null ? new SemesterDto
            {
                Id = user.Semester.Id,
                Value = user.Semester.Value
            } : null
        };
    }
}
```

### Authorization Attributes Usage

```csharp
// SmartInsights.API/Controllers/InquiriesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartInsights.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class InquiriesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous] // Override - allow anonymous for active inquiries
    public async Task<IActionResult> GetInquiries()
    {
        // Logic here
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Admin only
    public async Task<IActionResult> CreateInquiry([FromBody] CreateInquiryDto dto)
    {
        // Logic here
    }

    [HttpGet("my-responses")]
    [Authorize(Policy = "StudentOnly")] // Student only
    public async Task<IActionResult> GetMyResponses()
    {
        // Logic here
    }
}
```

---

## Implementation Phases

### Phase 1: Project Setup & Infrastructure (Week 1)

#### Step 1.1: Create Solution Structure

```bash
# Create solution
dotnet new sln -n SmartInsights

# Create projects
dotnet new classlib -n SmartInsights.Domain
dotnet new classlib -n SmartInsights.Application
dotnet new classlib -n SmartInsights.Infrastructure
dotnet new webapi -n SmartInsights.API

# Add projects to solution
dotnet sln add SmartInsights.Domain/SmartInsights.Domain.csproj
dotnet sln add SmartInsights.Application/SmartInsights.Application.csproj
dotnet sln add SmartInsights.Infrastructure/SmartInsights.Infrastructure.csproj
dotnet sln add SmartInsights.API/SmartInsights.API.csproj

# Add project references
cd SmartInsights.Application
dotnet add reference ../SmartInsights.Domain/SmartInsights.Domain.csproj

cd ../SmartInsights.Infrastructure
dotnet add reference ../SmartInsights.Application/SmartInsights.Application.csproj

cd ../SmartInsights.API
dotnet add reference ../SmartInsights.Application/SmartInsights.Application.csproj
dotnet add reference ../SmartInsights.Infrastructure/SmartInsights.Infrastructure.csproj
```

#### Step 1.2: Install NuGet Packages

```bash
# Domain - No packages needed (pure entities)

# Application
cd SmartInsights.Application
dotnet add package AutoMapper
dotnet add package FluentValidation
dotnet add package CsvHelper

# Infrastructure
cd ../SmartInsights.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Azure.AI.OpenAI
dotnet add package Hangfire
dotnet add package Hangfire.PostgreSql
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.PostgreSQL

# API
cd ../SmartInsights.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

#### Step 1.3: Setup Database Context

Create `ApplicationDbContext.cs` and configure all entities with fluent API.

#### Step 1.4: Create Initial Migration

```bash
cd SmartInsights.API
dotnet ef migrations add InitialCreate --project ../SmartInsights.Infrastructure
dotnet ef database update
```

#### Step 1.5: Setup Configuration

Update `appsettings.json` with database connection, JWT settings, Azure OpenAI credentials.

**Deliverables**:

- [ ] Solution structure created
- [ ] All packages installed
- [ ] Database created and migrated
- [ ] Configuration files setup

---

### Phase 2: Authentication & User Management

#### Step 2.1: Create Domain Entities

- Create all entity classes in `SmartInsights.Domain/Entities/`
- Create all enums in `SmartInsights.Domain/Enums/`

#### Step 2.2: Configure EF Core Relationships

```csharp
// SmartInsights.Infrastructure/Data/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relationships
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Program)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.ProgramId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Semester)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SemesterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

Repeat for all entities.

#### Step 2.3: Implement Generic Repository

```csharp
// SmartInsights.Infrastructure/Repositories/Repository.cs
using Microsoft.EntityFrameworkCore;
using SmartInsights.Application.Interfaces;
using SmartInsights.Domain.Common;
using System.Linq.Expressions;

namespace SmartInsights.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T?> GetByIdWithIncludesAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> query = _dbSet;

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<List<T>> GetPagedAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int page = 1,
        int pageSize = 20)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
            query = query.Where(filter);

        if (orderBy != null)
            query = orderBy(query);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        if (filter == null)
            return await _dbSet.CountAsync();

        return await _dbSet.CountAsync(filter);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
```

#### Step 2.4: Implement Auth Service

Create `AuthService.cs` as shown in Service Layer section.

#### Step 2.5: Create Auth Controller

```csharp
// SmartInsights.API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInsights.Application.DTOs.Auth;
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
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_CREDENTIALS",
                    message = ex.Message
                }
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(Guid.Parse(userId));
        return Ok(new { success = true, data = user });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT is stateless, so just return success
        // Client will discard the token
        return Ok(new { success = true, message = "Logged out successfully" });
    }
}
```

#### Step 2.6: Seed Initial Data

```csharp
// SmartInsights.Infrastructure/Data/DbSeeder.cs
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Departments
        if (!context.Departments.Any())
        {
            var departments = new List<Department>
            {
                new Department { Id = Guid.NewGuid(), Name = "Computer Science", Description = "CS and SE" },
                new Department { Id = Guid.NewGuid(), Name = "Electrical Engineering", Description = "EE" },
                new Department { Id = Guid.NewGuid(), Name = "Mechanical Engineering", Description = "ME" },
                new Department { Id = Guid.NewGuid(), Name = "Civil Engineering", Description = "CE" },
                new Department { Id = Guid.NewGuid(), Name = "Management Sciences", Description = "MBA/BBA" }
            };
            await context.Departments.AddRangeAsync(departments);
        }

        // Seed Programs
        if (!context.Programs.Any())
        {
            var programs = new List<Program>
            {
                new Program { Id = Guid.NewGuid(), Name = "BS Computer Science" },
                new Program { Id = Guid.NewGuid(), Name = "BS Software Engineering" },
                new Program { Id = Guid.NewGuid(), Name = "BS Electrical Engineering" }
            };
            await context.Programs.AddRangeAsync(programs);
        }

        // Seed Semesters
        if (!context.Semesters.Any())
        {
            var semesters = Enumerable.Range(1, 8)
                .Select(i => new Semester { Id = Guid.NewGuid(), Value = i.ToString() })
                .ToList();
            await context.Semesters.AddRangeAsync(semesters);
        }

        // Seed Themes
        if (!context.Themes.Any())
        {
            var themes = new List<Theme>
            {
                new Theme { Id = Guid.NewGuid(), Name = "Infrastructure" },
                new Theme { Id = Guid.NewGuid(), Name = "Academic" },
                new Theme { Id = Guid.NewGuid(), Name = "Administration" },
                new Theme { Id = Guid.NewGuid(), Name = "Student Life" },
                new Theme { Id = Guid.NewGuid(), Name = "Technology" },
                new Theme { Id = Guid.NewGuid(), Name = "Other" }
            };
            await context.Themes.AddRangeAsync(themes);
        }

        // Seed Admin User
        if (!context.Users.Any(u => u.Role == Role.Admin))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@kfueit.edu.pk",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = Role.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(adminUser);
        }

        await context.SaveChangesAsync();
    }
}
```

**Deliverables**:

- [ ] All entities created
- [ ] EF Core configurations complete
- [ ] Repository implemented
- [ ] Auth service implemented
- [ ] Login/logout endpoints working
- [ ] Seed data created

---

### Phase 3: Core CRUD Operations

#### Step 3.1: Implement Inquiry Service & Controller

- Create `IInquiryService` interface
- Implement `InquiryService`
- Create `InquiriesController` with all CRUD endpoints
- Test with Postman/Swagger

#### Step 3.2: Implement Input Service & Controller (Basic)

- Create `IInputService` interface
- Implement `InputService` (without AI processing for now)
- Create `InputsController` with basic CRUD
- Test input submission flow

#### Step 3.3: Implement Topic Service & Controller

- Create `ITopicService` interface
- Implement `TopicService`
- Create `TopicsController`
- Test topic listing and details

#### Step 3.4: Implement User Management

- Create `IUserService` interface
- Implement `UserService`
- Create `UsersController`
- Implement CSV import functionality

#### Step 3.5: Add Validation

```csharp
// SmartInsights.Application/Validators/CreateInputValidator.cs
using FluentValidation;
using SmartInsights.Application.DTOs.Inputs;

namespace SmartInsights.Application.Validators;

public class CreateInputValidator : AbstractValidator<CreateInputDto>
{
    public CreateInputValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Feedback body is required")
            .MinimumLength(20).WithMessage("Feedback must be at least 20 characters")
            .MaximumLength(2000).WithMessage("Feedback cannot exceed 2000 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid input type");

        RuleFor(x => x.InquiryId)
            .NotNull().When(x => x.Type == InputType.InquiryLinked)
            .WithMessage("Inquiry ID is required for inquiry responses");
    }
}

// Register in Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<CreateInputValidator>();
```

**Deliverables**:

- [ ] All CRUD endpoints implemented
- [ ] Validation added
- [ ] Basic testing complete
- [ ] Swagger documentation generated

---

### Phase 4: AI Integration

#### Step 4.1: Setup Azure OpenAI

- Create Azure OpenAI resource
- Deploy GPT-4 model
- Configure credentials in `appsettings.json`

#### Step 4.2: Implement AI Service

- Create `IAiProcessingService` interface
- Implement `AzureOpenAiService` with all methods
- Create prompt templates
- Test AI responses manually

#### Step 4.3: Setup Hangfire for Background Processing

```csharp
// Program.cs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// After app.Build()
app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<InputProcessingJob>(
    "process-pending-inputs",
    job => job.ProcessPendingInputs(),
    Cron.Minutely);
```

#### Step 4.4: Implement Input Processing Pipeline

- Create `InputProcessingJob` class
- Update `InputService.ProcessInputAsync()` with AI calls
- Test end-to-end: submit input → AI processing → results stored

#### Step 4.5: Implement Executive Summary Generation

- Add methods to generate inquiry summaries
- Add methods to generate topic summaries
- Create endpoints to trigger summary generation
- Test with real data

**Deliverables**:

- [ ] Azure OpenAI configured
- [ ] AI service fully implemented
- [ ] Background processing working
- [ ] Input processing pipeline complete
- [ ] Executive summaries generating

---

### Phase 5: Admin Features & Polish

#### Step 5.1: Implement Admin Reply Feature

- Add `AddAdminReplyAsync` to `InputService`
- Create `POST /api/inputs/{id}/reply` endpoint
- Test reply workflow

#### Step 5.2: Implement Identity Reveal Feature

- Add `RequestIdentityRevealAsync` to `InputService`
- Add `RespondToRevealRequestAsync` to `InputService`
- Create endpoints for both actions
- Test workflow

#### Step 5.3: Implement Dashboard Statistics

- Create `IDashboardService`
- Implement complex queries for stats
- Create `DashboardController`
- Test with large dataset

#### Step 5.4: Add Filters and Pagination

- Add filtering to all list endpoints
- Ensure pagination works correctly
- Add sorting options
- Test performance with 1000+ records

#### Step 5.5: Error Handling & Logging

```csharp
// SmartInsights.API/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = new { code = "UNAUTHORIZED", message = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = new { code = "INTERNAL_ERROR", message = "An error occurred" }
            });
        }
    }
}

// Register in Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Deliverables**:

- [ ] Admin reply working
- [ ] Identity reveal workflow complete
- [ ] Dashboard with statistics
- [ ] Filters and pagination working
- [ ] Error handling implemented
- [ ] Logging configured

---

### Phase 6: Testing & Optimization

#### Step 6.1: Create Test Data

- Write seed script for 1000+ test inputs
- Create multiple test inquiries
- Generate diverse feedback scenarios

#### Step 6.2: Performance Testing

- Test API response times under load
- Optimize slow queries
- Add database indexes if needed
- Test AI processing performance

#### Step 6.3: Frontend Integration Testing

- Connect Next.js frontend to backend
- Test all user flows end-to-end
- Fix any integration issues
- Validate data formats match

#### Step 6.4: Security Review

- Test authorization on all endpoints
- Validate input sanitization
- Check for SQL injection vulnerabilities
- Test JWT token handling

**Deliverables**:

- [ ] Test data created
- [ ] Performance optimizations applied
- [ ] Frontend integrated
- [ ] Security validated

---

### Phase 7: Deployment

#### Step 7.1: Setup Production Database

- Create PostgreSQL database on hosting provider
- Run migrations on production
- Seed initial data

#### Step 7.2: Configure Production Settings

- Update `appsettings.Production.json`
- Setup environment variables
- Configure CORS for frontend domain

#### Step 7.3: Deploy Backend

- Deploy to Azure App Service or similar
- Configure continuous deployment
- Setup health checks

#### Step 7.4: Monitor & Test

- Test all endpoints on production
- Monitor logs for errors
- Setup alerting for critical issues

**Deliverables**:

- [ ] Backend deployed to production
- [ ] Database migrated
- [ ] Monitoring configured
- [ ] Production testing complete

---

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartinsights;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-jwt-key-minimum-32-characters-long",
    "Issuer": "SmartInsights",
    "Audience": "SmartInsightsUsers",
    "ExpirationHours": 24
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-openai-api-key",
    "DeploymentName": "gpt-4o"
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "ServerName": "SmartInsights-BackgroundWorker"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Hangfire": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-frontend-domain.com"
    ]
  }
}
```

### Program.cs (Complete)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartInsights.Infrastructure.Data;
using SmartInsights.Application.Interfaces;
using SmartInsights.Application.Services;
using SmartInsights.Infrastructure.Repositories;
using SmartInsights.Infrastructure.AI;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Hangfire
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});

// Register Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInquiryService, InquiryService>();
builder.Services.AddScoped<IInputService, InputService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAiProcessingService, AzureOpenAiService>();

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Insights API",
        Version = "v1",
        Description = "API for KFUEIT Smart Insights Aggregator"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Schedule background jobs
RecurringJob.AddOrUpdate<InputProcessingJob>(
    "process-pending-inputs",
    job => job.ProcessPendingInputs(),
    Cron.Minutely);

app.Run();
```

---

## Summary Checklist

### Development Checklist

**Infrastructure & Setup**

- [ ] Solution and project structure created
- [ ] All NuGet packages installed
- [ ] Database connection configured
- [ ] Initial migration created and applied
- [ ] Seed data working

**Domain Layer**

- [ ] All entities created with properties
- [ ] All enums defined
- [ ] Entity relationships configured
- [ ] Base entity created

**Application Layer**

- [ ] All DTOs created
- [ ] All service interfaces defined
- [ ] All services implemented
- [ ] Validators created with FluentValidation
- [ ] AutoMapper profiles configured

**Infrastructure Layer**

- [ ] DbContext configured
- [ ] Generic repository implemented
- [ ] Azure OpenAI service implemented
- [ ] Prompt templates created
- [ ] Background jobs configured
- [ ] Logging configured

**API Layer**

- [ ] All controllers created
- [ ] JWT authentication configured
- [ ] Authorization policies set up
- [ ] Exception handling middleware
- [ ] CORS configured
- [ ] Swagger documentation

**Features**

- [ ] User authentication (login/logout)
- [ ] User management (CRUD, CSV import)
- [ ] Inquiry management (CRUD, send, close)
- [ ] Input submission and processing
- [ ] AI classification working
- [ ] AI quality scoring working
- [ ] Topic matching/creation working
- [ ] Executive summary generation working
- [ ] Admin replies to inputs
- [ ] Identity reveal workflow
- [ ] Dashboard statistics
- [ ] Filtering and pagination

**Testing & Quality**

- [ ] All endpoints tested with Postman/Swagger
- [ ] Test data created
- [ ] Performance testing done
- [ ] Security review completed
- [ ] Frontend integration tested

**Deployment**

- [ ] Production database setup
- [ ] Environment variables configured
- [ ] Backend deployed
- [ ] Monitoring configured

---

## Next Steps After MVP

1. **Notifications System**

   - Email notifications for admin replies
   - Real-time notifications for identity reveal requests

2. **Analytics Dashboard**

   - Trend analysis over time
   - Department-wise comparisons
   - Sentiment trends

3. **Advanced AI Features**

   - Automated action recommendations
   - Priority scoring improvements
   - Duplicate detection

4. **Export Features**

   - PDF report generation
   - Excel export for data analysis

5. **Mobile App**
   - React Native app for students

---

## Conclusion

This comprehensive plan covers every aspect of building the Smart Insights Aggregator backend with .NET 8. Follow each phase sequentially, testing thoroughly at each step. The plan is designed to be practical and achievable, with clear deliverables at each stage.

Key success factors:

- Follow Clean Architecture principles
- Write clean, maintainable code
- Test frequently
- Document as you go
- Commit regularly with meaningful messages

Good luck with your FYP! This is a solid, industry-standard architecture that will serve as an excellent portfolio project.
