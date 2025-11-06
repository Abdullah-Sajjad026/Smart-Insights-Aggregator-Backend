# Smart Insights Aggregator - Backend API

AI-powered feedback collection and analysis platform for KFUEIT University.

## 🚀 Quick Start

### Prerequisites

- **.NET SDK 8.0** or higher
- **PostgreSQL 16** or higher
- **Azure OpenAI** account (or compatible API endpoint)
- **Git**

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd Smart-Insights-Aggregator-Backend
```

2. **Configure appsettings**

Update `src/SmartInsights.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartinsights;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SmartInsightsAPI",
    "Audience": "SmartInsightsClient",
    "ExpiryInMinutes": 1440
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

3. **Create Database**
```bash
createdb smartinsights
```

4. **Run Migrations**
```bash
cd src/SmartInsights.API
dotnet ef database update --project ../SmartInsights.Infrastructure
```

5. **Seed Initial Data**

The application will seed data on first run, or you can manually seed:
```bash
dotnet run --seed
```

6. **Run the API**
```bash
dotnet run
```

API will be available at: https://localhost:7000

Swagger UI: https://localhost:7000 (root path in development)

## 📊 Default Credentials

After seeding, use these credentials:

**Admin Account:**
- Email: `admin@kfueit.edu.pk`
- Password: `Password123!`

**Student Account:**
- Email: `student@kfueit.edu.pk`
- Password: `Password123!`

## 🏗️ Architecture

### Clean Architecture Layers

```
SmartInsights.API          → Controllers, Program.cs
SmartInsights.Application  → Services, DTOs, Interfaces
SmartInsights.Infrastructure → Repositories, EF Core, External Services
SmartInsights.Domain       → Entities, Enums, Business Rules
```

### Key Features

✅ **48 API Endpoints** across 8 controllers
✅ **Role-based Authorization** (Admin/Student)
✅ **JWT Authentication**
✅ **Azure OpenAI Integration** for AI analysis
✅ **Anonymous Feedback** submission support
✅ **Real-time AI Processing** of inputs
✅ **Executive Summary Generation**
✅ **Identity Reveal Workflow**
✅ **Conversation System** (Admin-Student replies)

## 📡 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/validate` - Validate token

### Users (Admin only)
- `GET /api/users` - List users with filtering
- `GET /api/users/{id}` - Get user details
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user
- `POST /api/users/import-csv` - Bulk import from CSV
- `GET /api/users/stats` - User statistics

### Inquiries
- `GET /api/inquiries` - List inquiries
- `GET /api/inquiries/{id}` - Get inquiry details
- `POST /api/inquiries` - Create inquiry (Admin)
- `PUT /api/inquiries/{id}` - Update inquiry (Admin)
- `POST /api/inquiries/{id}/send` - Activate inquiry (Admin)
- `POST /api/inquiries/{id}/close` - Close inquiry (Admin)
- `DELETE /api/inquiries/{id}` - Delete inquiry (Admin)
- `GET /api/inquiries/my-inquiries` - Get user's inquiries (Admin)
- `GET /api/inquiries/{id}/stats` - Get inquiry statistics (Admin)

### Inputs (Feedback)
- `GET /api/inputs` - List with filtering (Admin)
- `GET /api/inputs/{id}` - Get input details
- `POST /api/inputs` - Submit feedback (Anonymous or Authenticated)
- `GET /api/inputs/my-inputs` - Get my submissions (Student)
- `POST /api/inputs/{id}/reveal-request` - Request identity reveal (Admin)
- `POST /api/inputs/{id}/reveal-respond` - Respond to reveal request (Student)
- `GET /api/inputs/{id}/replies` - Get conversation
- `POST /api/inputs/{id}/replies` - Add reply
- `GET /api/inputs/stats` - Input statistics (Admin)

### Topics
- `GET /api/topics` - List topics
- `GET /api/topics/{id}` - Get topic details
- `GET /api/topics/by-department/{id}` - Topics by department

### Departments
- `GET /api/departments` - List all
- `GET /api/departments/{id}` - Get details
- `POST /api/departments` - Create (Admin)
- `PUT /api/departments/{id}` - Update (Admin)
- `DELETE /api/departments/{id}` - Delete (Admin)

### Programs, Semesters, Themes
Similar CRUD endpoints for each resource.

## 🔐 Authentication

### Login Example
```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@kfueit.edu.pk",
    "password": "Password123!"
  }'
```

Response:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGc...",
    "email": "admin@kfueit.edu.pk",
    "firstName": "System",
    "lastName": "Administrator",
    "role": "Admin",
    "expiresAt": "2024-11-07T10:00:00Z"
  }
}
```

### Using the Token
```bash
curl -X GET https://localhost:7000/api/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 🤖 AI Processing

### How it Works

1. **Input Submission** → Status: Pending
2. **AI Analysis** → Status: Processing
   - Sentiment & Tone detection
   - Quality scoring (Urgency, Importance, Clarity, Quality, Helpfulness)
   - Theme extraction
   - Topic generation (for general feedback)
3. **Complete** → Status: Processed

### AI Analysis Results

Each input receives:
- **Sentiment**: Positive, Neutral, Negative
- **Tone**: Positive, Neutral, Negative
- **Quality Metrics**: 0.0 to 1.0 for each dimension
- **Overall Score**: Average of all metrics
- **Severity**: 1 (Low), 2 (Medium), 3 (High)
- **Theme**: Infrastructure, Academic, Technology, etc.
- **Topic**: Auto-generated or matched category

### Executive Summaries

Generated when inquiry/topic has 10+ inputs:
- Headline Insight
- Response Mix (sentiment breakdown)
- Key Takeaways
- Risks & Opportunities
- Prioritized Action Items

## 📝 Example Workflows

### 1. Submit Anonymous Feedback

```bash
curl -X POST https://localhost:7000/api/inputs \
  -H "Content-Type: application/json" \
  -d '{
    "body": "The WiFi in the library is very slow and disconnects frequently",
    "inquiryId": null
  }'
```

### 2. Create and Send Inquiry

```bash
# Create inquiry
curl -X POST https://localhost:7000/api/inquiries \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "body": "How satisfied are you with the lab equipment?",
    "departmentIds": ["dept-id-1"],
    "programIds": ["prog-id-1"],
    "semesterIds": ["sem-id-6"],
    "status": "Draft"
  }'

# Send inquiry (activate)
curl -X POST https://localhost:7000/api/inquiries/{id}/send \
  -H "Authorization: Bearer TOKEN"
```

### 3. Admin Reply to Feedback

```bash
curl -X POST https://localhost:7000/api/inputs/{id}/replies \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Thank you for your feedback. We are investigating this issue."
  }'
```

### 4. Request Identity Reveal

```bash
# Admin requests
curl -X POST https://localhost:7000/api/inputs/{id}/reveal-request \
  -H "Authorization: Bearer ADMIN_TOKEN"

# Student responds
curl -X POST https://localhost:7000/api/inputs/{id}/reveal-respond \
  -H "Authorization: Bearer STUDENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d 'true'
```

## 🧪 Testing

### Using Swagger UI

1. Navigate to https://localhost:7000
2. Click "Authorize" button
3. Enter: `Bearer YOUR_TOKEN`
4. Test endpoints interactively

### Using Postman

Import the Swagger JSON:
```
https://localhost:7000/swagger/v1/swagger.json
```

## 🗄️ Database Management

### Create Migration
```bash
dotnet ef migrations add MigrationName \
  --project src/SmartInsights.Infrastructure \
  --startup-project src/SmartInsights.API
```

### Apply Migrations
```bash
dotnet ef database update \
  --project src/SmartInsights.Infrastructure \
  --startup-project src/SmartInsights.API
```

### Reset Database
```bash
dotnet ef database drop --force \
  --project src/SmartInsights.Infrastructure \
  --startup-project src/SmartInsights.API

dotnet ef database update \
  --project src/SmartInsights.Infrastructure \
  --startup-project src/SmartInsights.API
```

## 📦 CSV Import Format

### Users CSV Format
```csv
Email,FirstName,LastName,Password,Role,Department,Program,Semester
john@example.com,John,Doe,Pass123!,Student,Computer Science,BS Computer Science,6
jane@example.com,Jane,Smith,Pass123!,Admin,,,
```

Import via:
```bash
curl -X POST https://localhost:7000/api/users/import-csv \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -F "file=@users.csv"
```

## 🔧 Troubleshooting

### "Azure OpenAI not configured"
Update `appsettings.json` with valid Azure OpenAI credentials.

### "Database connection failed"
Check PostgreSQL is running and connection string is correct.

### "JWT validation failed"
Ensure `SecretKey` is at least 32 characters and same in all environments.

### "Migration pending"
Run `dotnet ef database update` to apply migrations.

## 📚 Additional Documentation

- **PROJECT_OVERVIEW.md** - Complete project documentation
- **DOTNET_BACKEND_PLAN.md** - Detailed implementation plan
- **BACKEND_MODIFICATIONS_SUMMARY.md** - Plan improvements
- **IMPLEMENTATION_STATUS.md** - Current status and next steps
- **UPDATE_INSTRUCTIONS.md** - Manual update instructions

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Write/update tests
4. Submit a pull request

## 📄 License

Copyright © 2024 KFUEIT. All rights reserved.

## 🆘 Support

For issues and questions:
- Create an issue in the repository
- Contact: [your-email@kfueit.edu.pk]

---

**Built with ❤️ for KFUEIT University**
