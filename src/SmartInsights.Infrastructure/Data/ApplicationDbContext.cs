using Microsoft.EntityFrameworkCore;
using SmartInsights.Domain.Entities;
using SmartInsights.Infrastructure.Data.Configurations;

namespace SmartInsights.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<Input> Inputs => Set<Input>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<InputReply> InputReplies => Set<InputReply>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<InquiryDepartment> InquiryDepartments => Set<InquiryDepartment>();
    public DbSet<InquiryProgram> InquiryPrograms => Set<InquiryProgram>();
    public DbSet<InquirySemester> InquirySemesters => Set<InquirySemester>();
    public DbSet<InquiryFaculty> InquiryFaculties => Set<InquiryFaculty>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
