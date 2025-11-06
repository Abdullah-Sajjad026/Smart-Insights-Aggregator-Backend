
using Microsoft.EntityFrameworkCore;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Program> Programs { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Theme> Themes { get; set; }
    public DbSet<Inquiry> Inquiries { get; set; }
    public DbSet<Input> Inputs { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<InputReply> InputReplies { get; set; }
    public DbSet<InquiryDepartment> InquiryDepartments { get; set; }
    public DbSet<InquiryProgram> InquiryPrograms { get; set; }
    public DbSet<InquirySemester> InquirySemesters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId);
        modelBuilder.Entity<User>()
            .HasOne(u => u.Program)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.ProgramId);
        modelBuilder.Entity<User>()
            .HasOne(u => u.Semester)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SemesterId);

        // Inquiry
        modelBuilder.Entity<Inquiry>().HasKey(i => i.Id);
        modelBuilder.Entity<Inquiry>()
            .HasOne(i => i.CreatedBy)
            .WithMany(u => u.CreatedInquiries)
            .HasForeignKey(i => i.CreatedById);

        // Input
        modelBuilder.Entity<Input>().HasKey(i => i.Id);
        modelBuilder.Entity<Input>()
            .HasOne(i => i.User)
            .WithMany(u => u.Inputs)
            .HasForeignKey(i => i.UserId);
        modelBuilder.Entity<Input>()
            .HasOne(i => i.Inquiry)
            .WithMany(i => i.Inputs)
            .HasForeignKey(i => i.InquiryId);
        modelBuilder.Entity<Input>()
            .HasOne(i => i.Topic)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.TopicId);
        modelBuilder.Entity<Input>()
            .HasOne(i => i.Theme)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.ThemeId);

        // Topic
        modelBuilder.Entity<Topic>().HasKey(t => t.Id);
        modelBuilder.Entity<Topic>()
            .HasOne(t => t.Department)
            .WithMany(d => d.Topics)
            .HasForeignKey(t => t.DepartmentId);

        // InputReply
        modelBuilder.Entity<InputReply>().HasKey(ir => ir.Id);
        modelBuilder.Entity<InputReply>()
            .HasOne(ir => ir.Input)
            .WithMany(i => i.Replies)
            .HasForeignKey(ir => ir.InputId);
        modelBuilder.Entity<InputReply>()
            .HasOne(ir => ir.User)
            .WithMany()
            .HasForeignKey(ir => ir.UserId);

        // InquiryDepartment (Many-to-Many)
        modelBuilder.Entity<InquiryDepartment>().HasKey(id => new { id.InquiryId, id.DepartmentId });
        modelBuilder.Entity<InquiryDepartment>()
            .HasOne(id => id.Inquiry)
            .WithMany(i => i.InquiryDepartments)
            .HasForeignKey(id => id.InquiryId);
        modelBuilder.Entity<InquiryDepartment>()
            .HasOne(id => id.Department)
            .WithMany(d => d.InquiryDepartments)
            .HasForeignKey(id => id.DepartmentId);

        // InquiryProgram (Many-to-Many)
        modelBuilder.Entity<InquiryProgram>().HasKey(ip => new { ip.InquiryId, ip.ProgramId });
        modelBuilder.Entity<InquiryProgram>()
            .HasOne(ip => ip.Inquiry)
            .WithMany(i => i.InquiryPrograms)
            .HasForeignKey(ip => ip.InquiryId);
        modelBuilder.Entity<InquiryProgram>()
            .HasOne(ip => ip.Program)
            .WithMany(p => p.InquiryPrograms)
            .HasForeignKey(ip => ip.ProgramId);

        // InquirySemester (Many-to-Many)
        modelBuilder.Entity<InquirySemester>().HasKey(is => new { is.InquiryId, is.SemesterId });
        modelBuilder.Entity<InquirySemester>()
            .HasOne(is => is.Inquiry)
            .WithMany(i => i.InquirySemesters)
            .HasForeignKey(is => is.InquiryId);
        modelBuilder.Entity<InquirySemester>()
            .HasOne(is => is.Semester)
            .WithMany(s => s.InquirySemesters)
            .HasForeignKey(is => is.SemesterId);
    }
}
