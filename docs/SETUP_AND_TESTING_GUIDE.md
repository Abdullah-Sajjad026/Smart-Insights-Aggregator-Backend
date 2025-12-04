# Smart Insights Aggregator - Setup & Testing Guide

Complete guide to set up, run, and test the backend from scratch.

---

## Prerequisites

### Required Software

1. **.NET 8.0 SDK**
   ```bash
   dotnet --version  # Should show 8.0.x
   ```
   Download: https://dotnet.microsoft.com/download/dotnet/8.0

2. **PostgreSQL 14+**
   ```bash
   psql --version  # Should show 14.x or higher
   ```
   - **macOS**: `brew install postgresql@14`
   - **Windows**: Download from https://www.postgresql.org/download/
   - **Linux**: `sudo apt install postgresql-14`

3. **Azure OpenAI Account** (for AI features)
   - Sign up at: https://azure.microsoft.com/en-us/products/ai-services/openai-service
   - You'll need: Endpoint URL, API Key, and Deployment Name

4. **Optional Tools**
   - **Postman** or **Insomnia** - For API testing
   - **pgAdmin** or **DBeaver** - For database management
   - **Docker** (optional) - For containerized PostgreSQL

---

## Step 1: Clone and Build

```bash
# Clone the repository (if not already done)
git clone <your-repo-url>
cd Smart-Insights-Aggregator-Backend

# Verify the build works
dotnet clean
dotnet restore
dotnet build

# ✅ Should see: "Build succeeded"
```

---

## Step 2: Set Up PostgreSQL Database

### Option A: Local PostgreSQL Installation

**1. Start PostgreSQL Service**
```bash
# macOS
brew services start postgresql@14

# Linux
sudo systemctl start postgresql

# Windows
# PostgreSQL should auto-start, or use Services app
```

**2. Create Database**
```bash
# Connect to PostgreSQL
psql -U postgres

# In psql shell, run:
CREATE DATABASE smartinsights_dev;

# Verify
\l  # List all databases (should see smartinsights_dev)

# Exit
\q
```

**3. Test Connection**
```bash
psql -U postgres -d smartinsights_dev -c "SELECT version();"
# Should show PostgreSQL version
```

### Option B: Docker PostgreSQL (Easier!)

```bash
# Run PostgreSQL in Docker
docker run --name smartinsights-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=smartinsights_dev \
  -p 5432:5432 \
  -d postgres:14

# Verify it's running
docker ps | grep smartinsights-postgres

# To stop later: docker stop smartinsights-postgres
# To start again: docker start smartinsights-postgres
```

---

## Step 3: Configure Environment Variables

### Method 1: Update appsettings.Development.json (Recommended for Local)

Edit: `src/SmartInsights.API/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartinsights_dev;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
  },
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE-NAME.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "Temperature": "0.7",
    "MaxTokens": "2000",
    "MaxRetries": "3",
    "CacheExpirationHours": "24"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256!",
    "Issuer": "SmartInsightsAPI",
    "Audience": "SmartInsightsClient",
    "ExpiryInMinutes": 1440
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "http://localhost:5173"
    ]
  }
}
```

### Method 2: User Secrets (More Secure - Recommended for Production Keys)

```bash
cd src/SmartInsights.API

# Initialize user secrets
dotnet user-secrets init

# Set sensitive values
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=smartinsights_dev;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE-NAME.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR_AZURE_OPENAI_API_KEY"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"

# List all secrets
dotnet user-secrets list
```

### Method 3: Environment Variables

```bash
# Set for current session
export ConnectionStrings__DefaultConnection="Host=localhost;Database=smartinsights_dev;Username=postgres;Password=postgres"
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__ApiKey="your-api-key"
export AzureOpenAI__DeploymentName="gpt-4"

# Or add to ~/.bashrc or ~/.zshrc for persistence
```

---

## Step 4: Run Database Migrations

```bash
cd src/SmartInsights.API

# Create the database schema
dotnet ef database update

# ✅ Should see: "Done" or "Applying migration..."
```

**If you don't have EF Core tools installed:**
```bash
dotnet tool install --global dotnet-ef
```

**Verify Database Structure:**
```bash
psql -U postgres -d smartinsights_dev

\dt  # List all tables

# You should see:
# - Users
# - Departments
# - Programs
# - Semesters
# - Inquiries
# - Inputs
# - Topics
# - Themes
# - AIUsageLogs
# - InquiryDepartments
# - InquiryPrograms
# - InquirySemesters
# - hangfire tables

\q  # Exit
```

---

## Step 5: Seed Initial Data (Optional but Recommended)

Create a seed script or use pgAdmin to insert test data:

### Basic Seed Data SQL

```sql
-- Connect to database
\c smartinsights_dev

-- Insert Departments
INSERT INTO "Departments" ("Id", "Name", "Description", "CreatedAt") VALUES
('a0000000-0000-0000-0000-000000000001', 'Computer Science', 'CS Department', NOW()),
('a0000000-0000-0000-0000-000000000002', 'Software Engineering', 'SE Department', NOW()),
('a0000000-0000-0000-0000-000000000003', 'Electrical Engineering', 'EE Department', NOW());

-- Insert Programs
INSERT INTO "Programs" ("Id", "Name", "CreatedAt") VALUES
('b0000000-0000-0000-0000-000000000001', 'BSCS', NOW()),
('b0000000-0000-0000-0000-000000000002', 'BSSE', NOW()),
('b0000000-0000-0000-0000-000000000003', 'MSCS', NOW());

-- Insert Semesters
INSERT INTO "Semesters" ("Id", "Value", "CreatedAt") VALUES
('c0000000-0000-0000-0000-000000000001', '1st Semester', NOW()),
('c0000000-0000-0000-0000-000000000002', '2nd Semester', NOW()),
('c0000000-0000-0000-0000-000000000003', '3rd Semester', NOW()),
('c0000000-0000-0000-0000-000000000004', '4th Semester', NOW());

-- Insert Admin User (password: Admin123!)
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "Role", "Status", "CreatedAt", "UpdatedAt") VALUES
('d0000000-0000-0000-0000-000000000001', 'admin@kfueit.edu.pk', 'Admin', 'User', '$2a$11$YourBCryptHashHere', 'Admin', 'Active', NOW(), NOW());

-- Insert Test Student (password: Student123!)
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "Role", "Status", "DepartmentId", "ProgramId", "SemesterId", "CreatedAt", "UpdatedAt") VALUES
('d0000000-0000-0000-0000-000000000002', 'student@kfueit.edu.pk', 'Test', 'Student', '$2a$11$YourBCryptHashHere', 'Student', 'Active', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', NOW(), NOW());
```

**Note**: You'll need to generate proper BCrypt password hashes. See "Testing" section below for creating users via API.

---

## Step 6: Run the Application

```bash
cd src/SmartInsights.API

# Run in development mode
dotnet run

# Or watch mode (auto-restart on file changes)
dotnet watch run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Access Points:**
- **API Base**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **Health Check**: http://localhost:5000/health

---

## Step 7: Verify Everything Works

### 1. Health Check
```bash
curl http://localhost:5000/health

# Expected response:
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "azureOpenAI": "Healthy"  # May be Unhealthy if no API key
  }
}
```

### 2. Swagger UI
- Open: http://localhost:5000/swagger
- You should see all API endpoints documented

### 3. Hangfire Dashboard
- Open: http://localhost:5000/hangfire
- You should see the background job dashboard

---

## Testing the API

### Method 1: Using Swagger UI (Easiest!)

1. Open http://localhost:5000/swagger
2. Test endpoints interactively
3. Use "Try it out" button on any endpoint

### Method 2: Using cURL

#### A. Register a New User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@kfueit.edu.pk",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "User",
    "role": "Student",
    "departmentId": "a0000000-0000-0000-0000-000000000001",
    "programId": "b0000000-0000-0000-0000-000000000001",
    "semesterId": "c0000000-0000-0000-0000-000000000001"
  }'
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "...",
    "email": "testuser@kfueit.edu.pk",
    "firstName": "Test",
    "lastName": "User",
    "role": "Student"
  }
}
```

#### B. Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@kfueit.edu.pk",
    "password": "Test123!"
  }'
```

**Save the token from the response!**

#### C. Create Feedback (Authenticated)

```bash
# Replace YOUR_JWT_TOKEN with the token from login
curl -X POST http://localhost:5000/api/inputs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "body": "The WiFi in the library keeps disconnecting",
    "type": "General"
  }'
```

**Expected Response:**
```json
{
  "id": "...",
  "body": "The WiFi in the library keeps disconnecting",
  "type": "General",
  "status": "Pending",
  "createdAt": "2025-11-09T10:00:00Z"
}
```

#### D. Check Background Job Processing

Wait 10-30 seconds, then check Hangfire dashboard:
- Open: http://localhost:5000/hangfire
- Look for "ProcessInputAsync" jobs
- Check if they succeeded

#### E. Get Processed Input

```bash
curl -X GET http://localhost:5000/api/inputs/{inputId} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Expected Response (after AI processing):**
```json
{
  "id": "...",
  "body": "The WiFi in the library keeps disconnecting",
  "type": "General",
  "status": "Reviewed",
  "sentiment": "Negative",
  "tone": "Frustrated",
  "urgencyPct": 0.75,
  "importancePct": 0.80,
  "score": 0.82,
  "severity": 3,
  "topic": {
    "id": "...",
    "name": "Library WiFi Connectivity"
  }
}
```

### Method 3: Using Postman

**Import Collection:**
1. Create a new Postman collection
2. Add requests for each endpoint from Swagger
3. Set up environment variables:
   - `baseUrl`: http://localhost:5000
   - `token`: (will be set after login)

**Sample Requests:**

```
POST {{baseUrl}}/api/auth/register
POST {{baseUrl}}/api/auth/login
GET  {{baseUrl}}/api/departments
POST {{baseUrl}}/api/inputs (with Bearer token)
GET  {{baseUrl}}/api/inputs/{id} (with Bearer token)
POST {{baseUrl}}/api/inquiries (with Bearer token)
GET  {{baseUrl}}/api/inquiries (with Bearer token)
```

---

## Testing AI Features

### Prerequisites
- Azure OpenAI API key configured
- At least $5 credit in your Azure OpenAI account

### Test AI Processing

1. **Submit Feedback**
```bash
curl -X POST http://localhost:5000/api/inputs \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "body": "The cafeteria food quality has decreased significantly. Prices went up but portions are smaller.",
    "type": "General"
  }'
```

2. **Wait 10-30 seconds** (background job processes it)

3. **Check Hangfire Dashboard**
   - http://localhost:5000/hangfire
   - Look for "ProcessInputAsync" job
   - Should show "Succeeded"

4. **Retrieve Analyzed Input**
```bash
curl -X GET http://localhost:5000/api/inputs/{inputId} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected AI Analysis:**
- `sentiment`: "Negative"
- `tone`: "Disappointed"
- `urgencyPct`: ~0.5
- `importancePct`: ~0.7
- `score`: ~0.65
- `severity`: 2 (Medium)
- `topic`: "Cafeteria Quality and Pricing"

5. **Check AI Cost Tracking**
```bash
curl -X GET http://localhost:5000/api/monitoring/ai-usage \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Test Executive Summary Generation

1. **Create Multiple Inputs on Same Topic** (submit 10+ similar feedbacks)
2. **Wait for background job** "GenerateTopicSummaryAsync"
3. **Retrieve Topic with Summary**
```bash
curl -X GET http://localhost:5000/api/topics/{topicId} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Summary:**
```json
{
  "id": "...",
  "name": "Cafeteria Quality and Pricing",
  "inputCount": 12,
  "aiSummary": {
    "topics": ["Food Quality", "Pricing", "Portions"],
    "executiveSummaryData": {
      "headlineInsight": "83% of students report decreased food quality...",
      "keyTakeaways": "...",
      "risks": "...",
      "opportunities": "..."
    },
    "suggestedPrioritizedActions": [
      {
        "action": "Review cafeteria vendor contract",
        "impact": "HIGH",
        "responseCount": 12
      }
    ]
  }
}
```

---

## Monitoring & Debugging

### 1. Check Logs

```bash
# In the terminal where dotnet run is running
# Logs are printed to console

# Or check log files if configured:
tail -f logs/app-{date}.log
```

### 2. Hangfire Dashboard

- URL: http://localhost:5000/hangfire
- **Enqueued Jobs**: Jobs waiting to run
- **Processing**: Currently running jobs
- **Succeeded**: Completed jobs (green)
- **Failed**: Jobs with errors (red - click to see error details)
- **Recurring Jobs**: Scheduled jobs

**Useful Hangfire Features:**
- Retry failed jobs manually
- View job execution history
- See job parameters and exceptions
- Monitor worker threads

### 3. Database Queries

```bash
psql -U postgres -d smartinsights_dev

-- Check users
SELECT "Email", "Role", "Status" FROM "Users";

-- Check inputs
SELECT "Id", "Body", "Status", "Sentiment", "Score" FROM "Inputs";

-- Check topics
SELECT "Id", "Name", COUNT(*) as input_count
FROM "Topics" t
LEFT JOIN "Inputs" i ON t."Id" = i."TopicId"
GROUP BY t."Id", t."Name";

-- Check AI usage and cost
SELECT "Operation", COUNT(*) as calls, SUM("Cost") as total_cost
FROM "AIUsageLogs"
GROUP BY "Operation";
```

### 4. Health Check Monitoring

```bash
# Continuous health check
watch -n 5 'curl -s http://localhost:5000/health | jq'
```

---

## Common Issues & Solutions

### Issue 1: Database Connection Failed

**Error**: `Npgsql.NpgsqlException: Failed to connect to [::1]:5432`

**Solutions:**
```bash
# 1. Check PostgreSQL is running
brew services list | grep postgresql  # macOS
sudo systemctl status postgresql      # Linux

# 2. Verify credentials
psql -U postgres -d smartinsights_dev

# 3. Check connection string in appsettings.Development.json

# 4. Restart PostgreSQL
brew services restart postgresql@14   # macOS
sudo systemctl restart postgresql     # Linux
```

### Issue 2: Azure OpenAI Errors

**Error**: `RequestFailedException: Access denied due to invalid subscription key`

**Solutions:**
1. Verify API key in appsettings
2. Check Azure portal: your resource is active
3. Ensure you have credits remaining
4. Test endpoint URL format: `https://{resource-name}.openai.azure.com/`

**Temporary Workaround (Skip AI Processing):**
- Comment out AI service registration in Program.cs
- Or set a flag to skip AI processing in development

### Issue 3: Migrations Not Applied

**Error**: `Npgsql.PostgresException: relation "Users" does not exist`

**Solution:**
```bash
cd src/SmartInsights.API
dotnet ef database update --verbose
```

### Issue 4: Port Already in Use

**Error**: `System.IO.IOException: Failed to bind to address http://127.0.0.1:5000`

**Solution:**
```bash
# Find process using port 5000
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows

# Kill the process
kill -9 {PID}  # macOS/Linux

# Or change port in launchSettings.json
```

### Issue 5: Hangfire Dashboard Not Loading

**Solution:**
1. Ensure Hangfire tables exist in database:
```bash
psql -U postgres -d smartinsights_dev -c "\dt hangfire.*"
```

2. Check connection string has Hangfire configured

3. Restart application

---

## Production Deployment Checklist

Before deploying to production:

- [ ] **Secrets**: Move all sensitive data to environment variables or Azure Key Vault
- [ ] **Connection String**: Use production PostgreSQL server
- [ ] **JWT Secret**: Generate strong random key (64+ characters)
- [ ] **CORS**: Update allowed origins to production frontend URL
- [ ] **HTTPS**: Enable HTTPS redirect
- [ ] **Logging**: Configure Serilog with file/external logging
- [ ] **Database Backups**: Set up automated backups
- [ ] **Monitoring**: Add Application Insights or similar
- [ ] **Rate Limiting**: Enable rate limiting middleware
- [ ] **API Versioning**: Implement API versioning
- [ ] **Documentation**: Update README with production URLs

---

## Performance Testing

### Load Testing with `ab` (Apache Bench)

```bash
# Install ab
brew install apache-bench  # macOS
sudo apt install apache2-utils  # Linux

# Test login endpoint (100 requests, 10 concurrent)
ab -n 100 -c 10 -p login.json -T application/json \
  http://localhost:5000/api/auth/login
```

### Database Performance

```sql
-- Check slow queries
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Check table sizes
SELECT
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

## Next Steps

1. **Read the Learning Guide**: See `BACKEND_LEARNING_GUIDE.md` for deep dive into architecture
2. **Explore the Codebase**: Start with `Program.cs` and trace a request
3. **Build a Feature**: Try adding a new endpoint or enhancing existing ones
4. **Run Tests**: `dotnet test` (if tests are implemented)
5. **Deploy**: Follow production checklist above

---

## Useful Commands Reference

```bash
# Build & Run
dotnet clean
dotnet restore
dotnet build
dotnet run
dotnet watch run

# Migrations
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop --force

# Testing
dotnet test
dotnet test --logger "console;verbosity=detailed"

# Database
psql -U postgres -d smartinsights_dev
pg_dump -U postgres smartinsights_dev > backup.sql
psql -U postgres smartinsights_dev < backup.sql

# Docker
docker ps
docker logs smartinsights-postgres
docker exec -it smartinsights-postgres psql -U postgres
```

---

## Support & Resources

- **Learning Guide**: `BACKEND_LEARNING_GUIDE.md`
- **API Documentation**: http://localhost:5000/swagger (when running)
- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **.NET Docs**: https://learn.microsoft.com/en-us/aspnet/core/
- **PostgreSQL Docs**: https://www.postgresql.org/docs/
- **Azure OpenAI**: https://learn.microsoft.com/en-us/azure/ai-services/openai/

---

**Happy Coding! 🚀**
