using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InputConfiguration : IEntityTypeConfiguration<Input>
{
    public void Configure(EntityTypeBuilder<Input> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Sentiment)
            .HasConversion<string>();

        builder.Property(i => i.Tone)
            .HasConversion<string>();

        // Relationships
        builder.HasOne(i => i.User)
            .WithMany(u => u.Inputs)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Inquiry)
            .WithMany(inq => inq.Inputs)
            .HasForeignKey(i => i.InquiryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Topic)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.TopicId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Theme)
            .WithMany(th => th.Inputs)
            .HasForeignKey(i => i.ThemeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Replies)
            .WithOne(r => r.Input)
            .HasForeignKey(r => r.InputId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.InquiryId);
        builder.HasIndex(i => i.TopicId);
        builder.HasIndex(i => i.ThemeId);
        builder.HasIndex(i => i.Type);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Sentiment);
        builder.HasIndex(i => i.SeverityLevel);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(i => i.UpdatedAt);

        // Composite indexes for common AI processing queries
        builder.HasIndex(i => new { i.Status, i.AIProcessedAt })
            .HasDatabaseName("IX_Inputs_Status_AIProcessedAt");

        builder.HasIndex(i => new { i.TopicId, i.CreatedAt })
            .HasDatabaseName("IX_Inputs_TopicId_CreatedAt");

        builder.HasIndex(i => new { i.InquiryId, i.Status })
            .HasDatabaseName("IX_Inputs_InquiryId_Status");

        builder.HasIndex(i => new { i.Type, i.Status, i.CreatedAt })
            .HasDatabaseName("IX_Inputs_Type_Status_CreatedAt");
    }
}
