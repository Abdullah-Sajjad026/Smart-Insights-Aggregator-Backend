using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class AIUsageLogConfiguration : IEntityTypeConfiguration<AIUsageLog>
{
    public void Configure(EntityTypeBuilder<AIUsageLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Operation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PromptTokens)
            .IsRequired();

        builder.Property(a => a.CompletionTokens)
            .IsRequired();

        builder.Property(a => a.TotalTokens)
            .IsRequired();

        builder.Property(a => a.Cost)
            .IsRequired()
            .HasPrecision(18, 6); // High precision for cost tracking

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.Metadata)
            .HasMaxLength(2000);

        // Indexes for common queries
        builder.HasIndex(a => a.Operation);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.Operation, a.CreatedAt });
    }
}
