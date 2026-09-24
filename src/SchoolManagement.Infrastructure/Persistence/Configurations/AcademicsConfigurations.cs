using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(80);
        b.HasOne(x => x.HomeroomTeacher)
            .WithMany()
            .HasForeignKey(x => x.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(80);
        b.Property(x => x.Code).HasMaxLength(20);
    }
}

public class ClassSubjectTeacherConfiguration : IEntityTypeConfiguration<ClassSubjectTeacher>
{
    public void Configure(EntityTypeBuilder<ClassSubjectTeacher> b)
    {
        b.HasOne(x => x.SchoolClass).WithMany(c => c.Assignments).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany(s => s.Assignments).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Teacher).WithMany(t => t.Assignments).HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SchoolClassId, x.SubjectId, x.AcademicYear }).IsUnique();
    }
}

public class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> b)
    {
        b.HasOne(x => x.SchoolClass).WithMany(c => c.TimetableEntries).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}
