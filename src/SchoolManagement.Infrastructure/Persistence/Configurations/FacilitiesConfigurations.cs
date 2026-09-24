using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> b)
    {
        b.Property(x => x.Title).IsRequired().HasMaxLength(150);
        b.HasOne(x => x.TargetSchoolClass).WithMany().HasForeignKey(x => x.TargetSchoolClassId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> b)
    {
        b.Property(x => x.Title).IsRequired().HasMaxLength(150);
        b.Property(x => x.FilePath).IsRequired();
        b.HasOne(x => x.Student).WithMany(s => s.Documents).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
    }
}

public class LibraryLoanConfiguration : IEntityTypeConfiguration<LibraryLoan>
{
    public void Configure(EntityTypeBuilder<LibraryLoan> b)
    {
        b.HasOne(x => x.Book).WithMany(bk => bk.Loans).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.PurchaseValue).HasPrecision(18, 2);
    }
}
