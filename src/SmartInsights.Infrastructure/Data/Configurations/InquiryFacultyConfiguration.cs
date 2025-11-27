using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InquiryFacultyConfiguration : IEntityTypeConfiguration<InquiryFaculty>
{
    public void Configure(EntityTypeBuilder<InquiryFaculty> builder)
    {
        builder.HasKey(ifac => new { ifac.InquiryId, ifac.FacultyId });

        builder.HasOne(ifac => ifac.Inquiry)
            .WithMany(i => i.InquiryFaculties)
            .HasForeignKey(ifac => ifac.InquiryId);

        builder.HasOne(ifac => ifac.Faculty)
            .WithMany()
            .HasForeignKey(ifac => ifac.FacultyId);
    }
}
