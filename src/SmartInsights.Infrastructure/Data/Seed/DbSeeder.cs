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
            await SeedFacultiesAsync();
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

    private async Task SeedFacultiesAsync()
    {
        if (await _context.Faculties.AnyAsync())
        {
            _logger.LogInformation("Faculties already seeded");
            return;
        }

        var faculties = new[]
        {
            new Faculty
            {
                Id = Guid.NewGuid(),
                Name = "Faculty of Computing & IT",
                Description = "Faculty of Computing & Information Technology",
                CreatedAt = DateTime.UtcNow
            },
            new Faculty
            {
                Id = Guid.NewGuid(),
                Name = "Faculty of Engineering",
                Description = "Faculty of Engineering",
                CreatedAt = DateTime.UtcNow
            },
            new Faculty
            {
                Id = Guid.NewGuid(),
                Name = "Faculty of Management Sciences",
                Description = "Faculty of Management Sciences",
                CreatedAt = DateTime.UtcNow
            },
            new Faculty
            {
                Id = Guid.NewGuid(),
                Name = "Faculty of Natural Sciences",
                Description = "Faculty of Natural Sciences",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Faculties.AddRangeAsync(faculties);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Seeded {faculties.Length} faculties");
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _context.Departments.AnyAsync())
        {
            _logger.LogInformation("Departments already seeded");
            return;
        }

        var computingFaculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name == "Faculty of Computing & IT");
        var engineeringFaculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name == "Faculty of Engineering");
        var managementFaculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name == "Faculty of Management Sciences");
        var scienceFaculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name == "Faculty of Natural Sciences");

        var departments = new[]
        {
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Computer Science",
                Description = "Department of Computer Science & IT",
                FacultyId = computingFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Software Engineering",
                Description = "Department of Software Engineering",
                FacultyId = computingFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Electrical Engineering",
                Description = "Department of Electrical Engineering",
                FacultyId = engineeringFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Mechanical Engineering",
                Description = "Department of Mechanical Engineering",
                FacultyId = engineeringFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Civil Engineering",
                Description = "Department of Civil Engineering",
                FacultyId = engineeringFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Management Sciences",
                Description = "Department of Management Sciences",
                FacultyId = managementFaculty?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Physics",
                Description = "Department of Physics",
                FacultyId = scienceFaculty?.Id,
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

        var themes = new List<Theme>();
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "themes.csv");

        // Fallback to source path if running in development and file not copied to bin
        if (!File.Exists(csvPath))
        {
            // Try to find it relative to the project root (assuming we are in bin/Debug/net8.0)
            // This is a bit hacky but works for development
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/SmartInsights.Infrastructure"));
            csvPath = Path.Combine(projectRoot, "Data", "SeedData", "themes.csv");
        }

        if (File.Exists(csvPath))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(csvPath);
                // Skip header
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        var name = parts[0].Trim();
                        var typeStr = parts[1].Trim();
                        var description = parts.Length > 2 ? parts[2].Trim() : $"Auto-generated theme for {name}";

                        if (Enum.TryParse<ThemeType>(typeStr, true, out var type))
                        {
                            themes.Add(new Theme
                            {
                                Id = Guid.NewGuid(),
                                Name = name,
                                Type = type,
                                Description = description,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                _logger.LogInformation($"Loaded {themes.Count} themes from CSV");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading themes.csv");
                // Fallback to default themes if CSV fails
                themes.AddRange(GetDefaultThemes());
            }
        }
        else
        {
            _logger.LogWarning($"themes.csv not found at {csvPath}. Using default themes.");
            themes.AddRange(GetDefaultThemes());
        }

        if (themes.Any())
        {
            await _context.Themes.AddRangeAsync(themes);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {themes.Count} themes");
        }
    }

    private IEnumerable<Theme> GetDefaultThemes()
    {
        return new[]
        {
            new Theme { Id = Guid.NewGuid(), Name = "Infrastructure", Type = ThemeType.Facilities, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Academic", Type = ThemeType.Academic, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Technology", Type = ThemeType.Technology, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Facilities", Type = ThemeType.Facilities, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Administrative", Type = ThemeType.Administrative, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Social", Type = ThemeType.Social, CreatedAt = DateTime.UtcNow },
            new Theme { Id = Guid.NewGuid(), Name = "Other", Type = ThemeType.Other, CreatedAt = DateTime.UtcNow }
        };
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
