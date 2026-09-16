#region Purpose
// EF Core mapping for TimeWarp.Identity SiteSettings: schema identity, singleton row, store-CAS Version.
#endregion

#region Design
// Table identity.site_settings — alongside identity.principals, never inside principals.
// SiteSettings is port-backed (ISiteSettingsStore), not IAggregateRoot; Version is store-CAS plus
// .IsConcurrencyToken() like Principal. Task 227 dropped the trusted-tenants jsonb column; trust
// is Authentication:Entra:TenantId. Slice placement: mapping lives under
// features/settings/*-infrastructure.cs (settings product slice) even though the CLR type is
// TimeWarp.Identity — TWA0009 does not apply across assemblies.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Infrastructure;

using TimeWarp.Identity;

public sealed class SiteSettingsEntityTypeConfiguration : IEntityTypeConfiguration<SiteSettings>
{
  public const string SchemaName = "identity";
  public const string TableName = "site_settings";

  public void Configure(EntityTypeBuilder<SiteSettings> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(settings => settings.Id);

    builder.Property(settings => settings.Id)
      .HasConversion
      (
        id => id.Value,
        value => SiteSettingsId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(settings => settings.EntraSignInEnabled).IsRequired();
    builder.Property(settings => settings.EntraAllowBootstrap).IsRequired();
    builder.Property(settings => settings.PasskeyPromptMode).IsRequired();

    builder.Property(settings => settings.Version)
      .IsConcurrencyToken()
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
