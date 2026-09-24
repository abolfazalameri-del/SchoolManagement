using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.Name).IsUnique();

        // Defends "only one current year" at the database level too, not just in
        // AcademicYearService — a filtered unique index on IsCurrent means even a bug or a
        // future direct-SQL script cannot leave two years marked current at once. SQLite
        // supports partial indexes via a WHERE clause, which EF Core emits from HasFilter.
        b.HasIndex(x => x.IsCurrent).IsUnique().HasFilter("\"IsCurrent\" = 1");
    }
}

public class StudentEnrollmentHistoryConfiguration : IEntityTypeConfiguration<StudentEnrollmentHistory>
{
    public void Configure(EntityTypeBuilder<StudentEnrollmentHistory> b)
    {
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AcademicYear).WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        // One history row per student per year — re-running promotion for the same year updates, never duplicates.
        b.HasIndex(x => new { x.StudentId, x.AcademicYearId }).IsUnique();
    }
}
