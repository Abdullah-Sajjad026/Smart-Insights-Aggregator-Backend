using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Summary)
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // Relationships
        builder.HasOne(i => i.CreatedBy)
            .WithMany(u => u.CreatedInquiries)
            .HasForeignKey(i => i.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.InquiryDepartments)
            .WithOne(id => id.Inquiry)
            .HasForeignKey(id => id.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.InquiryPrograms)
            .WithOne(ip => ip.Inquiry)
            .HasForeignKey(ip => ip.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.InquirySemesters)
            .WithOne(isem => isem.Inquiry)
            .HasForeignKey(isem => isem.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Inputs)
            .WithOne(inp => inp.Inquiry)
            .HasForeignKey(inp => inp.InquiryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(i => i.CreatedById);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(i => i.UpdatedAt);

        // Composite index for active inquiry queries
        builder.HasIndex(i => new { i.Status, i.CreatedAt })
            .HasDatabaseName("IX_Inquiries_Status_CreatedAt");
    }
}
