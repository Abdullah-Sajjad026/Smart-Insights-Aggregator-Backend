using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InquirySemesterConfiguration : IEntityTypeConfiguration<InquirySemester>
{
    public void Configure(EntityTypeBuilder<InquirySemester> builder)
    {
        builder.HasKey(isem => new { isem.InquiryId, isem.SemesterId });

        builder.HasOne(isem => isem.Inquiry)
            .WithMany(i => i.InquirySemesters)
            .HasForeignKey(isem => isem.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(isem => isem.Semester)
            .WithMany(s => s.InquirySemesters)
            .HasForeignKey(isem => isem.SemesterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
