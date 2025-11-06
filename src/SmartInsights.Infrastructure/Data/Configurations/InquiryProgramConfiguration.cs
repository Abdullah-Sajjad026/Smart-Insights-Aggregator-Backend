using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InquiryProgramConfiguration : IEntityTypeConfiguration<InquiryProgram>
{
    public void Configure(EntityTypeBuilder<InquiryProgram> builder)
    {
        builder.HasKey(ip => new { ip.InquiryId, ip.ProgramId });

        builder.HasOne(ip => ip.Inquiry)
            .WithMany(i => i.InquiryPrograms)
            .HasForeignKey(ip => ip.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ip => ip.Program)
            .WithMany(p => p.InquiryPrograms)
            .HasForeignKey(ip => ip.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
