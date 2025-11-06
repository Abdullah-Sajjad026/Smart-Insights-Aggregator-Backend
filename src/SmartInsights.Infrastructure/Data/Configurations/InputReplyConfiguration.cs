using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InputReplyConfiguration : IEntityTypeConfiguration<InputReply>
{
    public void Configure(EntityTypeBuilder<InputReply> builder)
    {
        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(ir => ir.UserRole)
            .HasConversion<string>()
            .IsRequired();

        // Relationships
        builder.HasOne(ir => ir.Input)
            .WithMany(i => i.Replies)
            .HasForeignKey(ir => ir.InputId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ir => ir.User)
            .WithMany()
            .HasForeignKey(ir => ir.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ir => ir.InputId);
        builder.HasIndex(ir => new { ir.InputId, ir.CreatedAt });
    }
}
