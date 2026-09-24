using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> b)
    {
        b.Property(x => x.Title).IsRequired().HasMaxLength(150);
        b.Property(x => x.MaxScore).HasPrecision(6, 2);
        b.Property(x => x.PassingScore).HasPrecision(6, 2);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExamResultConfiguration : IEntityTypeConfiguration<ExamResult>
{
    public void Configure(EntityTypeBuilder<ExamResult> b)
    {
        b.Property(x => x.ScoreObtained).HasPrecision(6, 2);
        b.HasOne(x => x.Exam).WithMany(e => e.Results).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Student).WithMany(s => s.ExamResults).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
    }
}

public class ReportCardConfiguration : IEntityTypeConfiguration<ReportCard>
{
    public void Configure(EntityTypeBuilder<ReportCard> b)
    {
        b.Property(x => x.OverallAverage).HasPrecision(6, 2);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Term }).IsUnique();
    }
}

public class ReportCardSubjectLineConfiguration : IEntityTypeConfiguration<ReportCardSubjectLine>
{
    public void Configure(EntityTypeBuilder<ReportCardSubjectLine> b)
    {
        b.Property(x => x.Score).HasPrecision(6, 2);
        b.HasOne(x => x.ReportCard).WithMany(r => r.SubjectLines).HasForeignKey(x => x.ReportCardId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
