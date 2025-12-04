# Smart Insights Aggregator - Complete Project Overview

**Version:** 1.0
**Last Updated:** November 5, 2024
**Project Type:** Final Year Project (FYP)
**Institution:** KFUEIT (Khwaja Fareed University of Engineering & Information Technology)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Problem Statement & Solution](#problem-statement--solution)
3. [System Overview](#system-overview)
4. [Core Features](#core-features)
5. [Technical Architecture](#technical-architecture)
6. [Project Structure](#project-structure)
7. [Development Status](#development-status)
8. [Getting Started Guide](#getting-started-guide)
9. [Team & Roles](#team--roles)
10. [Timeline & Milestones](#timeline--milestones)

---

## Executive Summary

**Smart Insights Aggregator** is an AI-powered feedback collection and analysis platform designed to help KFUEIT University gather, analyze, and act on anonymous student feedback at scale.

### What Problem Does It Solve?

Traditional feedback systems suffer from:
- **Low participation** due to fear of identification
- **Manual analysis** that takes weeks and is subjective
- **Difficulty identifying patterns** in large volumes of feedback
- **No structured communication** between students and administration

### Our Solution

A comprehensive platform that enables:
- **100% Anonymous Feedback**: Students can speak freely without fear
- **AI-Powered Analysis**: Automatic categorization, sentiment analysis, and insight generation
- **Two Types of Feedback**:
  - **Targeted Inquiries**: Admin-created questions for specific departments/programs
  - **General Feedback**: Student-initiated feedback on any topic
- **Aggregate Intelligence**: Executive summaries with risks, opportunities, and prioritized actions
- **Controlled Two-Way Communication**: Admin replies and optional identity reveal

### Key Differentiators

1. **Aggregate Analysis Only**: No individual feedback stalking - focus is on patterns
2. **AI-Generated Topics**: General feedback is automatically grouped into emerging themes
3. **Quality Scoring**: Every input is scored on urgency, importance, clarity, and helpfulness
4. **Executive Summary Format**: Structured insights designed for decision-makers
5. **Student Control**: Identity reveal is optional and always requires student approval

---

## Problem Statement & Solution

### The Problem

KFUEIT University faces challenges in gathering honest, actionable feedback from students:

1. **Fear of Retaliation**: Students hesitate to provide critical feedback if they can be identified
2. **Survey Fatigue**: Traditional surveys have low response rates
3. **Analysis Overhead**: Manually analyzing hundreds of feedback responses is impractical
4. **No Follow-up**: Feedback often goes into a black hole with no response or action
5. **Scattered Insights**: Important patterns are lost in the noise of individual complaints
6. **Department Silos**: Feedback isn't properly routed to relevant departments

### Our Solution Approach

#### For Students:
- **Anonymous Submission**: Submit feedback without logging in or being tracked
- **Two Feedback Channels**:
  - Respond to specific inquiries (e.g., "How's the lab equipment?")
  - Submit general feedback anytime (e.g., "WiFi keeps disconnecting")
- **See Admin Responses**: Get feedback on your feedback
- **Optional Identity Reveal**: Choose to reveal identity only when needed for follow-up

#### For Administrators:
- **Create Targeted Inquiries**: Ask specific questions to specific audiences (e.g., CS 6th semester students)
- **AI-Powered Insights**: Get executive summaries instead of reading 500 individual responses
- **Topic Discovery**: See emerging themes from general feedback automatically
- **Quality Filtering**: Focus on high-severity, high-importance feedback first
- **Action Recommendations**: AI suggests prioritized actions with impact assessment
- **Controlled Communication**: Reply to feedback or request identity reveal when necessary

#### Technical Solution:
- **Frontend**: Modern Next.js web application with admin and student portals
- **Backend**: .NET 8 REST APIs with PostgreSQL database
- **AI Integration**: Azure OpenAI (GPT-4) for classification, analysis, and summarization
- **Background Processing**: Automatic processing of feedback and generation of insights

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         WEB BROWSER                              │
│                                                                  │
│  ┌─────────────────────┐         ┌──────────────────────┐      │
│  │   Student Portal     │         │    Admin Portal      │      │
│  │  (Next.js Frontend)  │         │  (Next.js Frontend)  │      │
│  └─────────────────────┘         └──────────────────────┘      │
└──────────────────────────┬────────────────┬────────────────────┘
                           │                │
                           │  HTTPS/REST    │
                           ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    .NET 8 WEB API BACKEND                        │
│                                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │   Auth   │  │ Inquiry  │  │  Input   │  │  Topic   │       │
│  │   API    │  │   API    │  │   API    │  │   API    │       │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘       │
│                                                                  │
│  ┌──────────────────────┐         ┌──────────────────────┐     │
│  │   AI Processing      │         │   Background Jobs    │     │
│  │   Service (Azure)    │         │   (Hangfire)         │     │
│  └──────────────────────┘         └──────────────────────┘     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │  EF Core
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    PostgreSQL DATABASE                           │
│                                                                  │
│   ┌────────┐  ┌──────────┐  ┌────────┐  ┌────────┐            │
│   │ Users  │  │ Inquiries│  │ Inputs │  │ Topics │            │
│   └────────┘  └──────────┘  └────────┘  └────────┘            │
│   ┌────────┐  ┌──────────┐  ┌──────────────────────┐          │
│   │ Depts  │  │ Programs │  │  AI Analysis (JSON)  │          │
│   └────────┘  └──────────┘  └──────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │  API Calls
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              AZURE OPENAI SERVICE (GPT-4)                        │
│                                                                  │
│  • Sentiment & Tone Analysis                                    │
│  • Quality Scoring (Urgency, Importance, Clarity)              │
│  • Topic Clustering & Naming                                    │
│  • Executive Summary Generation                                 │
│  • Action Recommendations                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow

#### Student Submits Feedback:
```
1. Student writes feedback → Frontend validates (20-2000 chars)
2. POST to /api/inputs → Backend saves to DB (status: PENDING)
3. Background job picks up → Sends to Azure OpenAI
4. AI returns: sentiment, tone, quality scores, theme, severity
5. Backend updates DB (status: PROCESSED)
6. Admin can now view the analyzed feedback
```

#### Admin Creates Inquiry:
```
1. Admin writes inquiry → Selects target (dept/program/semester)
2. POST to /api/inquiries → Backend saves (status: DRAFT)
3. Admin clicks "Send" → Status becomes ACTIVE
4. Students see inquiry in their portal → Can submit responses
5. Responses flow through AI processing (same as above)
6. Once enough responses → Background job generates Executive Summary
7. Admin views inquiry detail → Sees AI-generated insights
```

#### AI Generates Topic:
```
1. General input processed → AI extracts theme (e.g., "WiFi Issues")
2. Backend checks existing topics → Finds match OR creates new topic
3. Input assigned to topic → Topic stats updated
4. When topic has 10+ inputs → Generate aggregate analysis
5. Admin views topic detail → Sees Executive Summary
```

---

## Core Features

### 1. Anonymous Feedback Submission

**What**: Students can submit feedback without logging in
**Why**: Encourages honest feedback without fear of retaliation
**How**:
- No authentication required for submission
- System tracks department/program/semester but not identity
- Optional identity reveal only with student approval

### 2. Targeted Inquiries

**What**: Admins create specific questions for specific audiences
**Example**: "CS 6th semester students: How's your FYP supervision experience?"
**Features**:
- Target by department, program, and/or semester
- Set start and end dates
- Track response rate
- Get AI-powered executive summary

**Inquiry Structure**:
```json
{
  "body": "Single question text (20-2000 chars)",
  "targetDepartments": ["Computer Science", "Software Engineering"],
  "targetPrograms": ["BS Computer Science"],
  "targetSemesters": ["6", "7", "8"],
  "startDate": "2024-11-05",
  "endDate": "2024-11-20"
}
```

### 3. General Feedback (Open Input)

**What**: Students can submit feedback anytime on any topic
**AI Processing**:
- Extracts theme (e.g., "Infrastructure", "Academic", "Technology")
- Matches to existing topic OR creates new topic
- Groups similar feedback together automatically

**Example Topics Created**:
- "Library WiFi Connectivity Issues" (25 inputs)
- "Cafeteria Food Quality & Pricing" (18 inputs)
- "Exam Scheduling Conflicts" (12 inputs)

### 4. AI-Powered Quality Scoring

Every input gets scored on 5 dimensions (0-100%):

| Metric | What It Measures |
|--------|------------------|
| **Urgency** | How time-sensitive is this issue? |
| **Importance** | How many students does this affect? |
| **Clarity** | How well-articulated is the feedback? |
| **Quality** | How actionable and constructive is it? |
| **Helpfulness** | How useful is this for decision-making? |

**Severity Calculation**:
- **HIGH (3)**: Score ≥ 75% - Needs immediate attention
- **MEDIUM (2)**: Score 50-75% - Should be addressed soon
- **LOW (1)**: Score < 50% - Monitor but not urgent

### 5. Executive Summary Generation

AI analyzes all feedback for an inquiry or topic and generates:

#### Headline Insight
> "Critical infrastructure gaps in CS lab affecting 80% of students"

#### Response Mix
> "245 responses: 80 negative, 120 neutral, 45 positive"

#### Key Takeaways (2-3 paragraphs)
Detailed analysis with specific examples and patterns from the feedback.

#### Risks (What could go wrong)
> "Continued use of outdated equipment may lead to decreased learning outcomes, student frustration, and potential safety hazards."

#### Opportunities (What could be improved)
> "Upgrading lab infrastructure presents opportunity to improve student satisfaction, enhance practical learning, and attract more students to the program."

#### Suggested Prioritized Actions
```json
[
  {
    "action": "Replace lab computers with minimum 16GB RAM",
    "impact": "HIGH",
    "challenges": "Budget allocation (~$50k for 30 machines)",
    "responseCount": 180,
    "reasoning": "85% of students cite inadequate RAM as primary bottleneck"
  },
  {
    "action": "Update software to latest versions",
    "impact": "MEDIUM",
    "challenges": "Minimal - IT team can do this",
    "responseCount": 95,
    "reasoning": "Many report compatibility issues with outdated software"
  }
]
```

### 6. Admin-Student Communication

#### Admin Replies
- Admin can reply to any input
- Reply is visible to the student
- Reply is anonymous (student doesn't see which admin replied)

#### Identity Reveal Workflow
1. Admin sees important feedback → Clicks "Request Identity Reveal"
2. Student gets notification → Can approve or deny
3. If approved → Admin sees student name and can follow up
4. If denied → Feedback remains anonymous

### 7. Dashboard & Analytics

**Admin Dashboard** shows:
- Total inputs, inquiries, topics
- Sentiment distribution (positive/neutral/negative)
- Importance breakdown (high/medium/low)
- Top themes
- Department-wise breakdown
- Urgent inputs requiring attention
- Identity reveal requests pending

**Student Dashboard** shows:
- History of submitted feedback
- Admin replies
- Identity reveal requests
- Active inquiries to respond to

---

## Technical Architecture

### Technology Stack

#### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| Next.js | 14.2.5 | React framework with SSR |
| Material-UI | 5.16.6 | UI component library |
| React Query | 3.39.3 | Server state management |
| Axios | 1.7.2 | HTTP client |
| Zod | 3.23.8 | Form validation |
| Tailwind CSS | 3.4.1 | Utility-first CSS |

#### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Web API framework |
| Entity Framework Core | 8.0 | ORM for database access |
| PostgreSQL | 16 | Primary database |
| Hangfire | Latest | Background job processing |
| JWT | Latest | Authentication |
| BCrypt | Latest | Password hashing |
| Serilog | Latest | Structured logging |

#### AI & ML
| Service | Purpose |
|---------|---------|
| Azure OpenAI (GPT-4) | Primary AI service |
| Alternative: Google Gemini | Backup option |

### Architecture Pattern

**Clean Architecture** with 4 layers:

```
┌─────────────────────────────────────────┐
│  SmartInsights.API (Controllers)        │ ← HTTP Layer
├─────────────────────────────────────────┤
│  SmartInsights.Application (Services)   │ ← Business Logic
├─────────────────────────────────────────┤
│  SmartInsights.Infrastructure (Data)    │ ← Database & External
├─────────────────────────────────────────┤
│  SmartInsights.Domain (Entities)        │ ← Core Models
└─────────────────────────────────────────┘
```

**Benefits**:
- **Testability**: Each layer can be tested independently
- **Maintainability**: Clear separation of concerns
- **Flexibility**: Easy to swap implementations (e.g., change database)
- **Industry Standard**: Recognized pattern in enterprise software

### Database Schema

**10 Main Tables**:

1. **Users**: Students and admins
2. **Departments**: CS, EE, ME, CE, etc.
3. **Programs**: BS Computer Science, BS Software Engineering, etc.
4. **Semesters**: 1, 2, 3, 4, 5, 6, 7, 8
5. **Inquiries**: Admin-created questions
6. **InquiryDepartments**: Many-to-many relationship
7. **InquiryPrograms**: Many-to-many relationship
8. **InquirySemesters**: Many-to-many relationship
9. **Inputs**: Student feedback
10. **Topics**: Auto-generated categories
11. **Themes**: Predefined themes (Infrastructure, Academic, etc.)
12. **InputReplies**: Admin-student conversation threads

**Key Relationships**:
- One inquiry has many inputs
- One topic has many inputs
- One user has many inputs
- One inquiry targets multiple departments/programs/semesters

### Security

1. **Authentication**: JWT-based token authentication
2. **Authorization**: Role-based access control (Admin vs Student)
3. **Password Security**: BCrypt hashing with salt
4. **Anonymity Protection**: User identity hidden by default
5. **CORS**: Configured for frontend domain only
6. **SQL Injection**: Protected by Entity Framework parameterization
7. **XSS**: Sanitized inputs on frontend and backend

---

## Project Structure

### Repository Organization

```
/Users/mac/Projects/Work/FYP/
│
├── frontend/                    # Next.js Frontend
│   ├── constants/
│   ├── modules/
│   │   ├── shared/             # Shared components, layouts
│   │   ├── input/              # Input submission
│   │   ├── inquiry/            # Inquiry management
│   │   └── user/               # Authentication
│   ├── pages/
│   │   ├── admin/              # Admin portal pages
│   │   ├── student/            # Student portal pages
│   │   └── input/              # Input submission pages
│   ├── public/
│   ├── styles/
│   └── package.json
│
├── backend/                     # .NET 8 Backend (To be created)
│   ├── src/
│   │   ├── SmartInsights.Domain/
│   │   ├── SmartInsights.Application/
│   │   ├── SmartInsights.Infrastructure/
│   │   └── SmartInsights.API/
│   └── tests/
│
├── PROJECT_OVERVIEW.md          # This file
├── DOTNET_BACKEND_PLAN.md       # Detailed backend implementation plan
└── frontend/PROJECT_DOCUMENTATION.md  # Frontend-specific docs
```

### Key Documentation Files

| File | Purpose | Audience |
|------|---------|----------|
| **PROJECT_OVERVIEW.md** | Complete project overview | Everyone (new developers, stakeholders) |
| **DOTNET_BACKEND_PLAN.md** | Step-by-step backend implementation | Backend developers |
| **frontend/PROJECT_DOCUMENTATION.md** | Frontend structure and features | Frontend developers |

---

## Development Status

### ✅ Completed (Frontend MVP)

**Admin Portal**:
- [x] Dashboard with statistics
- [x] Inquiries list and detail pages
- [x] Topics list and detail pages
- [x] Input detail page with admin actions
- [x] Create inquiry dialog
- [x] AI analysis visualization (Executive Summary format)
- [x] Quality metrics display

**Student Portal**:
- [x] Home/history page
- [x] Active inquiries list
- [x] Input submission form (general feedback)
- [x] Inquiry response flow

**Shared Components**:
- [x] Sidebar navigation
- [x] ImportanceBadge component
- [x] SentimentIndicator component
- [x] ThemeChip component
- [x] Loading components
- [x] Logo component

**Mock Data**:
- [x] Complete mock data matching reference structure
- [x] Realistic sample inquiries, inputs, topics
- [x] AI analysis examples in Executive Summary format

### 🚧 In Progress

**Backend Development**:
- [ ] Project setup and architecture
- [ ] Database schema creation
- [ ] Authentication implementation
- [ ] API endpoints development
- [ ] AI service integration
- [ ] Background job processing

### 📋 Pending

**Frontend**:
- [ ] Real authentication (currently no auth)
- [ ] API integration (remove mock data)
- [ ] Error boundaries and handling
- [ ] Comprehensive testing

**Backend**:
- [ ] Complete .NET 8 project setup
- [ ] All API endpoints
- [ ] Azure OpenAI integration
- [ ] CSV user import
- [ ] Deployment configuration

**Testing**:
- [ ] Unit tests (backend)
- [ ] Integration tests
- [ ] End-to-end tests
- [ ] Performance testing
- [ ] Security audit

**Deployment**:
- [ ] CI/CD pipeline
- [ ] Production environment setup
- [ ] Monitoring and alerting
- [ ] Backup strategy

---

## Getting Started Guide

### For New Developers

#### Prerequisites
- **Node.js** 18.x or higher
- **npm** or **yarn**
- **.NET SDK** 8.0
- **PostgreSQL** 16
- **Git**
- **Visual Studio Code** or **Visual Studio 2022**

#### Setup Steps

##### 1. Clone the Repository
```bash
git clone <repository-url>
cd FYP
```

##### 2. Frontend Setup
```bash
cd frontend
npm install
npm run dev
```
Open http://localhost:3000

##### 3. Backend Setup (Once Created)
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run --project src/SmartInsights.API
```
Backend runs on http://localhost:5000

##### 4. Database Setup
```bash
# Install PostgreSQL 16
# Create database
createdb smartinsights

# Run migrations (from backend project)
dotnet ef database update
```

#### Understanding the Codebase

**Start Here**:
1. Read this file (PROJECT_OVERVIEW.md) completely
2. Read frontend/PROJECT_DOCUMENTATION.md for frontend details
3. Read DOTNET_BACKEND_PLAN.md for backend implementation guide
4. Explore the frontend code starting from `pages/admin/dashboard.js`
5. Check mock data structure in `modules/shared/shared.mock-data-v2.js`

**Key Concepts to Understand**:
- Difference between Inquiry-Linked and General inputs
- Why inquiries have single `body` field (not title + description)
- How AI analysis works (3-stage processing)
- Executive Summary format
- Quality scoring dimensions
- Topic auto-generation

#### Development Workflow

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Follow existing code patterns
   - Use TypeScript (frontend) and C# (backend)
   - Write clean, documented code

3. **Test Locally**
   - Test in browser (frontend)
   - Test with Swagger (backend)
   - Verify mock data flows

4. **Commit with Meaningful Messages**
   ```bash
   git add .
   git commit -m "feat: add user CSV import functionality"
   ```

5. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   ```

---

## Team & Roles

### Project Structure

**Project Owner**: KFUEIT University Administration
**Project Type**: Final Year Project (FYP)
**Institution**: KFUEIT

### Roles (To Be Filled)

- **Project Lead**: Overall coordination and decision-making
- **Frontend Developer**: Next.js UI development
- **Backend Developer**: .NET 8 API development
- **AI/ML Engineer**: OpenAI integration and prompt engineering
- **Database Administrator**: PostgreSQL schema and optimization
- **DevOps Engineer**: Deployment and CI/CD
- **QA Engineer**: Testing and quality assurance

### Collaboration Tools

- **Version Control**: Git (GitHub/GitLab/Bitbucket)
- **Project Management**: Jira/Trello/Asana
- **Communication**: Slack/Discord/Teams
- **Documentation**: Markdown files in repository
- **API Testing**: Postman/Swagger
- **CI/CD**: GitHub Actions/Azure DevOps

---

## Timeline & Milestones

### Phase 1: Foundation (Weeks 1-2) ✅ DONE
- [x] Project planning and architecture design
- [x] Frontend MVP with mock data
- [x] UI/UX design finalization
- [x] Theme and styling

### Phase 2: Backend Setup (Weeks 3-4) 🔄 CURRENT
- [ ] .NET 8 project structure
- [ ] Database schema creation
- [ ] Authentication implementation
- [ ] Basic CRUD endpoints
- [ ] EF Core configurations

**Deliverable**: Working backend with auth and basic endpoints

### Phase 3: AI Integration (Weeks 5-6)
- [ ] Azure OpenAI account setup
- [ ] AI service implementation
- [ ] Input classification logic
- [ ] Quality scoring implementation
- [ ] Topic clustering algorithm
- [ ] Prompt engineering and testing

**Deliverable**: AI-powered input processing working end-to-end

### Phase 4: Advanced Features (Weeks 7-8)
- [ ] Executive summary generation
- [ ] Background job processing (Hangfire)
- [ ] Admin reply functionality
- [ ] Identity reveal workflow
- [ ] CSV user import
- [ ] Dashboard statistics

**Deliverable**: All core features functional

### Phase 5: Integration (Week 9)
- [ ] Connect frontend to real backend APIs
- [ ] Remove mock data
- [ ] End-to-end testing
- [ ] Bug fixes
- [ ] Performance optimization

**Deliverable**: Fully integrated application

### Phase 6: Testing & Polish (Week 10)
- [ ] Comprehensive testing (unit, integration, E2E)
- [ ] Security audit
- [ ] Performance testing
- [ ] UI/UX refinements
- [ ] Documentation updates

**Deliverable**: Production-ready application

### Phase 7: Deployment (Week 11)
- [ ] Production environment setup
- [ ] Database deployment
- [ ] Backend deployment (Azure/AWS)
- [ ] Frontend deployment (Vercel/Netlify)
- [ ] CI/CD pipeline
- [ ] Monitoring setup

**Deliverable**: Live application

### Phase 8: Training & Handover (Week 12)
- [ ] User training for admins
- [ ] Documentation for maintenance
- [ ] Knowledge transfer
- [ ] Go-live support

**Deliverable**: System in production use

---

## Technical Decisions & Rationale

### Why .NET 8 for Backend?

**Reasons**:
1. **Performance**: Native compilation, better than Node.js for CPU-intensive AI processing
2. **Type Safety**: Strong typing reduces bugs
3. **Enterprise Grade**: Trusted by large organizations
4. **Azure Integration**: Seamless with Azure OpenAI
5. **Career Value**: .NET is in high demand in job market
6. **Clean Architecture**: Well-suited for layered architecture
7. **Tooling**: Excellent IDE support (Visual Studio, Rider)

**Alternatives Considered**:
- ❌ Node.js/Express: Less suitable for CPU-intensive AI tasks
- ❌ Python/FastAPI: Weaker typing, less enterprise adoption
- ❌ Java/Spring: More verbose, steeper learning curve

### Why Next.js for Frontend?

**Reasons**:
1. **React Ecosystem**: Large community and component libraries
2. **SSR Support**: Better SEO and initial load performance
3. **API Routes**: Can build simple APIs in same codebase
4. **Developer Experience**: Hot reload, TypeScript support
5. **Production Ready**: Used by major companies

### Why PostgreSQL?

**Reasons**:
1. **Open Source**: No licensing costs
2. **Robust**: ACID compliance, mature
3. **JSON Support**: Good for storing AI analysis
4. **Performance**: Excellent query optimization
5. **Scalability**: Handles millions of rows easily

**Alternatives Considered**:
- ❌ SQL Server: Licensing costs
- ❌ MySQL: Weaker JSON support
- ❌ MongoDB: Not ideal for relational data

### Why Azure OpenAI?

**Reasons**:
1. **Microsoft Integration**: Seamless with .NET
2. **Enterprise Support**: SLAs and support
3. **Data Privacy**: Data stays in your region
4. **Latest Models**: Access to GPT-4 and newer
5. **Cost Control**: Better billing controls

**Alternatives Considered**:
- ❌ OpenAI Direct: Less enterprise features
- ❌ Google Gemini: Newer, less mature
- ❌ Self-hosted Models: Too resource-intensive

### Design Decisions

#### Single `body` Field for Inquiries
Instead of separate title and description:
- Simpler data model
- More flexible for admins
- Better for AI processing (single context)

#### No Theme for Inquiry Inputs
Inquiry responses don't need themes because:
- Inquiry itself provides context
- Reduces redundancy
- Focuses on quality over categorization

#### Aggregate Analysis Only
Focus on patterns, not individuals:
- Protects student privacy
- More actionable for admins
- Reduces bias

#### Executive Summary Format
Structured insights (Headline, Risks, Opportunities):
- Executive-friendly
- Action-oriented
- Consistent format

---

## Common Questions (FAQ)

### Q: How is student anonymity maintained?
**A**: Students don't need to log in to submit feedback. The system tracks metadata (department, program, semester) but not identity. Identity reveal is optional and requires student approval.

### Q: What if a student submits inappropriate content?
**A**: Admins can see all feedback. If content violates policy, admins can:
1. Request identity reveal
2. If approved, take appropriate action
3. If denied, content can be flagged and archived

### Q: How often does AI analysis run?
**A**:
- **Individual inputs**: Processed within minutes of submission
- **Aggregate analysis**: Generated when inquiry/topic has 10+ inputs
- **Updates**: Re-generated when significant new inputs arrive

### Q: Can students see AI analysis of their feedback?
**A**: No. Students only see:
- Their own submitted feedback
- Admin replies to their feedback
- Status (pending/processed)

AI analysis is for admins only.

### Q: What happens if AI service is down?
**A**:
- Inputs are saved to database immediately
- Background jobs retry processing
- Manual fallback available for admins

### Q: How much does Azure OpenAI cost?
**A**: Approximate costs (as of 2024):
- GPT-4: ~$0.03 per 1K input tokens, ~$0.06 per 1K output tokens
- For 1000 inputs per month: ~$50-100/month
- Executive summaries: ~$5-10 per summary

### Q: Can the system scale to multiple universities?
**A**: Currently single-tenant (KFUEIT only). To make multi-tenant:
- Add Organization entity
- Add organizationId to all tables
- Implement organization-level isolation
- Update authentication to support multiple orgs

Estimated effort: 2-3 weeks

### Q: What's the difference between Inquiry and Topic?
**A**:
- **Inquiry**: Admin-created question with targeted audience
- **Topic**: Auto-generated category for general feedback

Example:
- Inquiry: "How's the lab equipment?" (admin asks)
- Topic: "WiFi Connectivity Issues" (AI discovers from general feedback)

### Q: Can admins create topics manually?
**A**: In current design, no. Topics are AI-generated only. However, future enhancement could allow manual topic creation and merging.

### Q: How do I add a new department?
**A**: Use the CSV import feature or admin API:
```http
POST /api/departments/bulk-create
{
  "departments": [
    { "name": "Physics", "description": "Physics Department" }
  ]
}
```

---

## Troubleshooting

### Frontend Issues

**Problem**: Sidebar is empty
**Solution**: Authentication not implemented yet. Mock data workaround needed.

**Problem**: Mock data not loading
**Solution**: Check `modules/shared/shared.mock-data-v2.js` is imported correctly.

**Problem**: Styles not applying
**Solution**: Run `npm run dev` (not `npm start`) to enable hot reload.

### Backend Issues (When Implemented)

**Problem**: Database connection fails
**Solution**: Check connection string in `appsettings.json`.

**Problem**: Migration fails
**Solution**: Delete migrations folder and recreate: `dotnet ef migrations add InitialCreate`

**Problem**: JWT token not working
**Solution**: Check secret key length (minimum 32 characters).

### AI Integration Issues

**Problem**: Azure OpenAI returns 429 (rate limit)
**Solution**: Implement retry logic with exponential backoff.

**Problem**: AI responses are poor quality
**Solution**: Refine prompts in `PromptTemplates.cs`.

---

## Next Steps

### Immediate (This Week)
1. ✅ Complete frontend MVP (DONE)
2. ✅ Create comprehensive documentation (DONE)
3. 🔄 Start backend project setup
4. 🔄 Create database schema
5. 🔄 Implement authentication

### Short Term (Next 2 Weeks)
1. Complete all CRUD endpoints
2. Integrate Azure OpenAI
3. Implement background job processing
4. Test AI classification

### Medium Term (Next Month)
1. Connect frontend to backend
2. End-to-end testing
3. Performance optimization
4. Security audit

### Long Term (Next 2 Months)
1. Deploy to production
2. User training
3. Monitor and iterate
4. Add advanced features

---

## Resources

### Documentation
- **Frontend Docs**: `/frontend/PROJECT_DOCUMENTATION.md`
- **Backend Plan**: `/DOTNET_BACKEND_PLAN.md`
- **This File**: `/PROJECT_OVERVIEW.md`

### External Resources
- [Next.js Documentation](https://nextjs.org/docs)
- [Material-UI Components](https://mui.com/material-ui/getting-started/)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Tools
- [Postman](https://www.postman.com/) - API testing
- [Swagger UI](https://swagger.io/tools/swagger-ui/) - API documentation
- [pgAdmin](https://www.pgadmin.org/) - PostgreSQL management
- [Visual Studio Code](https://code.visualstudio.com/) - Code editor

---

## Contact & Support

### Project Team
- **Project Lead**: [Your Name]
- **Email**: [your.email@kfueit.edu.pk]
- **Repository**: [GitHub/GitLab URL]

### Getting Help
1. Check documentation first (this file + others)
2. Search existing issues in repository
3. Ask in team chat (Slack/Discord/Teams)
4. Create issue in repository with detailed description

### Reporting Issues
When reporting issues, include:
- What you were trying to do
- What happened (actual result)
- What you expected (expected result)
- Steps to reproduce
- Screenshots if applicable
- Error messages (full stack trace)

---

## Conclusion

Smart Insights Aggregator is a comprehensive, AI-powered feedback system designed to revolutionize how KFUEIT University collects and acts on student feedback. With a solid architecture, modern tech stack, and clear implementation plan, this project is positioned for success.

**Key Takeaways**:
- **Anonymous & Safe**: Students can provide honest feedback
- **AI-Powered**: Automatic analysis saves countless hours
- **Action-Oriented**: Focus on insights and prioritized actions
- **Scalable**: Architecture supports growth
- **Production-Ready**: Following industry best practices

This document, combined with the detailed backend plan and frontend documentation, provides everything a new developer needs to understand and contribute to the project.

**Good luck with development! 🚀**

---

**Document Version**: 1.0
**Last Updated**: November 5, 2024
**Status**: Living Document (will be updated as project evolves)
