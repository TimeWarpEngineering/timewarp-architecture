#region Purpose
// EF Core mapping for TimeWarp.Identity Principal: schema identity, TypedId key, store-CAS Version token.
#endregion

#region Design
// Principal is a port-backed durable entity (IPrincipalStore), not an IAggregateRoot teaching
// aggregate like Profile. Mapping lives under features/identity/*-infrastructure.cs so
// web-infrastructure globs it; the library stays EF-free (task 104-032).
// Schema "identity" / table "principals" — schema-per-slice on the single PostgresDbContext
// (ADR-0009). Version uses .IsConcurrencyToken() as the DB race belt beside the store's own
// EntityVersion.Next CAS; GoldenDbContext does not auto-bump Version because Principal is not
// IAggregateRoot (store-CAS authority — soft-gate 104-032).
// PropertyAccessMode.Property on Version so PropertyEntry OriginalValue/CurrentValue writes
// work regardless of backing-field naming when EfPrincipalStore attaches snapshots.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

using TimeWarp.Identity;

public sealed class PrincipalEntityTypeConfiguration : IEntityTypeConfiguration<Principal>
{
  public const string SchemaName = "identity";
  public const string TableName = "principals";

  public void Configure(EntityTypeBuilder<Principal> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(principal => principal.Id);

    builder.Property(principal => principal.Id)
      .HasConversion
      (
        id => id.Value,
        value => PrincipalId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(principal => principal.Kind).IsRequired();
    builder.Property(principal => principal.TrustTier).IsRequired();
    builder.Property(principal => principal.IsQuarantined).IsRequired();
    builder.Property(principal => principal.CreatedAt).IsRequired();
    builder.Property(principal => principal.DisplayName);

    builder.Property(principal => principal.Version)
      .IsConcurrencyToken()
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
