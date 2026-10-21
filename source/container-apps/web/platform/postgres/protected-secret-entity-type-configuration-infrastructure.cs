#region Purpose
// EF Core mapping for protected secrets: schema secrets, one row per name.
#endregion

#region Design
// Table secrets.protected_secrets. Ciphertext is text because Data Protection payloads vary in
// length. The suffix is short on purpose: it is the only piece of the secret stored in the clear,
// and it exists so admin can show "ends in …xxxx". Discovered by ApplyConfigurationsFromAssembly.
// This file lives under platform/postgres so a generated app with the flag off does not compile it.
#endregion

namespace TimeWarp.Architecture.Secrets;

/// <summary>Maps <see cref="ProtectedSecret"/> to <c>secrets.protected_secrets</c>.</summary>
public sealed class ProtectedSecretEntityTypeConfiguration : IEntityTypeConfiguration<ProtectedSecret>
{
  /// <summary>Schema name.</summary>
  public const string SchemaName = "secrets";

  /// <summary>Table name.</summary>
  public const string TableName = "protected_secrets";

  /// <inheritdoc />
  public void Configure(EntityTypeBuilder<ProtectedSecret> builder)
  {
    builder.ToTable(TableName, SchemaName);
    builder.HasKey(secret => secret.Name);
    builder.Property(secret => secret.Name).HasMaxLength(128).IsRequired();
    builder.Property(secret => secret.Purpose).HasMaxLength(256).IsRequired();
    builder.Property(secret => secret.Ciphertext).IsRequired();
    builder.Property(secret => secret.Suffix).HasMaxLength(8).IsRequired();
    builder.Property(secret => secret.SetAt).IsRequired();
    builder.Property(secret => secret.SetByPrincipalId).IsRequired();
  }
}
