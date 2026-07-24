#region Purpose
// EF Core mapping for the Profile teaching aggregate: table/schema, TypedId key, Version concurrency token.
#endregion

#region Design
// Profile is the template's reference aggregate (web-domain/aggregates/profile). Mapping lives here
// under the feature tree as *-infrastructure.cs so web-infrastructure globs it with the rest of the
// product infrastructure layer — not hand-registered next to PostgresDbContext.
// Schema-per-slice on a single PostgresDbContext: table and PostgreSQL schema are both "profiles"
// (parent 113 decision: one context, slice schemas via ToTable schema, until a second module needs
// its own DbContext). EnsureCreated (startup hosted service) materializes this schema.
// TypedId: ProfileId stores as Guid via explicit conversion (Id is get-only; EF binds it through the
// private constructor parameter). Host also calls ConfigureTypedIdConventions so other TypedIds in
// the model convert the same way without per-property ceremony.
// Version: IsConcurrencyToken is now applied for free by GoldenAggregateVersionConvention
// (registered by GoldenDbContext's sealed ConfigureConventions, task 121) for every mapped
// IAggregateRoot — Profile no longer calls .IsConcurrencyToken() itself (one-party contract,
// ADR-0009 update). The explicit UsePropertyAccessMode(PropertyAccessMode.Property) call below is
// redundant with the convention's own pin but stays as a local exemplar. Private setters elsewhere
// stay PreferFieldDuringConstruction.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Infrastructure;

using TimeWarp.Architecture.Aggregates.Profiles;

public sealed class ProfileEntityTypeConfiguration : IEntityTypeConfiguration<Profile>
{
  public const string SchemaName = "profiles";
  public const string TableName = "profiles";

  public void Configure(EntityTypeBuilder<Profile> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(profile => profile.Id);

    builder.Property(profile => profile.Id)
      .HasConversion
      (
        id => id.Value,
        value => ProfileId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(profile => profile.DisplayName)
      .HasMaxLength(Profile.MaxDisplayNameLength)
      .IsRequired();

    builder.Property(profile => profile.Language).IsRequired();
    builder.Property(profile => profile.Region).IsRequired();
    builder.Property(profile => profile.Theme).IsRequired();
    builder.Property(profile => profile.Notifications).IsRequired();

    builder.Property(profile => profile.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
