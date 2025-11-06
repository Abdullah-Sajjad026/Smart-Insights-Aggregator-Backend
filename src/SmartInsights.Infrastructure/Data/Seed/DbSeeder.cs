using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsights.Domain.Entities;
using SmartInsights.Domain.Enums;

namespace SmartInsights.Infrastructure.Data.Seed;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbSeeder> _logger;
    private readonly string _defaultPassword = "Password123!"; // BCrypt hash will be: $2a$11$...

    public DbSeeder(ApplicationDbContext context, ILogger<DbSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Seed in order of dependencies
            await SeedDepartmentsAsync();
            await SeedProgramsAsync();
            await SemestersAsync();
            await SeedThemesAsync();
            await SeedUsersAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _context.Departments.AnyAsync())
        {
            _logger.LogInformation("Departments already seeded");
            return;
        }

        var departments = new[]
        {
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Computer Science",
                Description = "Department of Computer Science & IT",
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Software Engineering",
                Description = "Department of Software Engineering",
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Electrical Engineering",
                Description = "Department of Electrical Engineering",
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Mechanical Engineering",
                Description = "Department of Mechanical Engineering",
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Civil Engineering",
                Description = "Department of Civil Engineering",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Departments.AddRangeAsync(departments);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {departments.Length} departments");
    }

    private async Task SeedProgramsAsync()
    {
        if (await _context.Programs.AnyAsync())
        {
            _logger.LogInformation("Programs already seeded");
            return;
        }

        var programs = new[]
        {
            new Program { Id = Guid.NewGuid(), Name = "BS Computer Science", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "BS Software Engineering", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "BS Electrical Engineering", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "BS Mechanical Engineering", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "BS Civil Engineering", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "MS Computer Science", CreatedAt = DateTime.UtcNow },
            new Program { Id = Guid.NewGuid(), Name = "MS Software Engineering", CreatedAt = DateTime.UtcNow }
        };

        await _context.Programs.AddRangeAsync(programs);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {programs.Length} programs");
    }

    private async Task SemestersAsync()
    {
        if (await _context.Semesters.AnyAsync())
        {
            _logger.LogInformation("Semesters already seeded");
            return;
        }

        var semesters = new List<Semester>();
        for (int i = 1; i <= 8; i++)
        {
            semesters.Add(new Semester
            {
                Id = Guid.NewGuid(),
                Value = i.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.Semesters.AddRangeAsync(semesters);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {semesters.Count} semesters");
    }

    private async Task SeedThemesAsync()
    {
        if (await _context.Themes.AnyAsync())
        {
            _logger.LogInformation("Themes already seeded");
            return;
        }

        var themes = new[]
        {
            new Theme { Id = Guid.NewGuid(), Name = "Infrastructure", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Academic", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Technology", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Facilities", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Administrative", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Social", CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Other", CreatedAt = DateTime.UtcNow }
        };

        await _context.Themes.AddRangeAsync(themes);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {themes.Length} themes");
    }

    private async Task SeedUsersAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Users already seeded");
            return;
        }

        var csDept = await _context.Departments.FirstAsync(d => d.Name == "Computer Science");
        var csProgram = await _context.Programs.FirstAsync(p => p.Name == "BS Computer Science");
        var semester6 = await _context.Semesters.FirstAsync(s => s.Value == "6");

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(_defaultPassword);

        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@kfueit.edu.pk",
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = passwordHash,
                Role = Role.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "student@kfueit.edu.pk",
                FirstName = "Test",
                LastName = "Student",
                PasswordHash = passwordHash,
                Role = Role.Student,
                Status = UserStatus.Active,
                DepartmentId = csDept.Id,
                ProgramId = csProgram.Id,
                SemesterId = semester6.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {users.Length} users (default password: {_defaultPassword})");
    }
}
