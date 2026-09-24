using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class StudentAttendanceRecordConfiguration : IEntityTypeConfiguration<StudentAttendanceRecord>
{
    public void Configure(EntityTypeBuilder<StudentAttendanceRecord> b)
    {
        b.HasOne(x => x.Student).WithMany(s => s.AttendanceRecords).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        // One attendance mark per student per day — recording twice updates, never duplicates.
        b.HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
    }
}

public class StaffAttendanceRecordConfiguration : IEntityTypeConfiguration<StaffAttendanceRecord>
{
    public void Configure(EntityTypeBuilder<StaffAttendanceRecord> b)
    {
        b.HasOne(x => x.Teacher).WithMany(t => t.AttendanceRecords).HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TeacherId, x.Date }).IsUnique();
    }
}
