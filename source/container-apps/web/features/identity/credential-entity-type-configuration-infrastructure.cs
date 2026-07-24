#region Purpose
// EF Core mapping for TimeWarp.Identity Credential: schema identity, bytea handle/material, unique (Type, Handle).
#endregion

#region Design
// Independent entity (not OwnsMany of Principal) so credential rows can be looked up by handle
// without loading the principal — matches IPrincipalStore.FindCredentialByHandleAsync.
// Handle and PublicMaterial are private fields (HandleField / PublicMaterialField) with
// copy-on-get properties; field access keeps EF from treating ToArray() copies as mutations
// and preserves D8 (callers never share storage with the row).
// Unique index on (Type, Handle) is the DB belt for atomic handle uniqueness that
// InMemoryPrincipalStore enforces via HandleIndex under WriteLock; EfPrincipalStore still
// checks first so callers get InvalidOperationException, not a raw unique-violation.
// Version store-CAS + .IsConcurrencyToken() same rationale as PrincipalEntityTypeConfiguration
// (not IAggregateRoot; store owns EntityVersion.Next).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

using TimeWarp.Identity;

public sealed class CredentialEntityTypeConfiguration : IEntityTypeConfiguration<Credential>
{
  public const string SchemaName = PrincipalEntityTypeConfiguration.SchemaName;
  public const string TableName = "credentials";

  public void Configure(EntityTypeBuilder<Credential> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(credential => credential.Id);

    builder.Property(credential => credential.Id)
      .HasConversion
      (
        id => id.Value,
        value => CredentialId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(credential => credential.PrincipalId)
      .HasConversion
      (
        id => id.Value,
        value => PrincipalId.From(value)
      )
      .IsRequired();

    builder.Property(credential => credential.Type).IsRequired();

    builder.Property(credential => credential.Handle)
      .HasField("HandleField")
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasColumnType("bytea")
      .IsRequired();

    builder.Property(credential => credential.PublicMaterial)
      .HasField("PublicMaterialField")
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasColumnType("bytea")
      .IsRequired();

    builder.Property(credential => credential.CreatedAt).IsRequired();
    builder.Property(credential => credential.RevokedAt);
    builder.Property(credential => credential.Label);

    builder.Property(credential => credential.Version)
      .IsConcurrencyToken()
      .UsePropertyAccessMode(PropertyAccessMode.Property);

    builder.HasIndex(credential => new { credential.Type, credential.Handle }).IsUnique();
    builder.HasIndex(credential => credential.PrincipalId);
  }
}
