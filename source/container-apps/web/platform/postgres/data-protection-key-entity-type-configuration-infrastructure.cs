#region Purpose
// Maps the ASP.NET Data Protection key ring onto Postgres.
#endregion

#region Design
// Schema data_protection, table keys. Columns stay Id, FriendlyName, and Xml: those are the names
// PersistKeysToDbContext reads. Xml is required. The application name that isolates rings lives
// on DataProtectionDefaults, not on this table.
#endregion

namespace TimeWarp.Architecture.Persistence;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Maps <see cref="DataProtectionKey"/> to <c>data_protection.keys</c>.</summary>
public sealed class DataProtectionKeyEntityTypeConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
  /// <summary>Schema that holds the key ring.</summary>
  public const string SchemaName = "data_protection";

  /// <summary>Table that holds one row per key.</summary>
  public const string TableName = "keys";

  /// <inheritdoc />
  public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
  {
    builder.ToTable(TableName, SchemaName);
    builder.HasKey(key => key.Id);
    builder.Property(key => key.Xml).IsRequired();
  }
}
