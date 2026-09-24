using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.Property(x => x.StudentNumber).IsRequired().HasMaxLength(30);
        b.HasIndex(x => x.StudentNumber).IsUnique();
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);

        b.HasOne(x => x.SchoolClass)
            .WithMany(c => c.Students)
            .HasForeignKey(x => x.SchoolClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> b)
    {
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
    }
}

public class StudentParentConfiguration : IEntityTypeConfiguration<StudentParent>
{
    public void Configure(EntityTypeBuilder<StudentParent> b)
    {
        b.HasOne(x => x.Student).WithMany(s => s.StudentParents).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Parent).WithMany(p => p.StudentParents).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StudentId, x.ParentId }).IsUnique();
    }
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.MonthlySalary).HasPrecision(18, 2);

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(x => x.Username).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.PasswordSalt).IsRequired();
    }
}
