using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(50);
        b.Property(x => x.EntityName).IsRequired().HasMaxLength(80);
        b.HasIndex(x => x.TimestampUtc);
        // Deliberately no soft-delete, no FK constraints tying it to live rows — the audit trail
        // must remain readable even after the record it describes is later removed.
    }
}

public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.FilePath).IsRequired();
    }
}

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Key).IsUnique();
    }
}
