using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Summary)
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // Relationships
        builder.HasOne(t => t.Department)
            .WithMany(d => d.Topics)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Inputs)
            .WithOne(i => i.Topic)
            .HasForeignKey(i => i.TopicId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.DepartmentId);
        builder.HasIndex(t => t.CreatedAt);
    }
}
