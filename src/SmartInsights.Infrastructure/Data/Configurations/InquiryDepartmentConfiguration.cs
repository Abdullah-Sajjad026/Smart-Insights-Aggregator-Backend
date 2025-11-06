using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInsights.Domain.Entities;

namespace SmartInsights.Infrastructure.Data.Configurations;

public class InquiryDepartmentConfiguration : IEntityTypeConfiguration<InquiryDepartment>
{
    public void Configure(EntityTypeBuilder<InquiryDepartment> builder)
    {
        builder.HasKey(id => new { id.InquiryId, id.DepartmentId });

        builder.HasOne(id => id.Inquiry)
            .WithMany(i => i.InquiryDepartments)
            .HasForeignKey(id => id.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(id => id.Department)
            .WithMany(d => d.InquiryDepartments)
            .HasForeignKey(id => id.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
