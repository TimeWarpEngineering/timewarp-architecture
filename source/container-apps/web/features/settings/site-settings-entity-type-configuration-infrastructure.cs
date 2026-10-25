#region Purpose
// EF Core mapping for TimeWarp.Identity SiteSettings: schema identity, singleton row, store-CAS Version.
#endregion

#region Design
// Table identity.site_settings — alongside identity.principals, never inside principals.
// SiteSettings is port-backed (ISiteSettingsStore), not IAggregateRoot; Version is store-CAS plus
// .IsConcurrencyToken() like Principal. Task 306 stores tenant acceptance on the row:
// EntraTenantAcceptanceMode (int), EntraAcceptedTenants (jsonb entries via EntraTenantIds), and
// EntraTenantAcceptanceInitialized (false on existing rows so the seeder copies a GUID authority
// once). Slice placement: mapping lives under features/settings/*-infrastructure.cs (settings
// product slice) even though the CLR type is TimeWarp.Identity — TWA0009 does not apply across
// assemblies. AssistantName (task 361) is a required varchar(AssistantNameRules.MaxLength); the
// AddSiteSettingsAssistantName migration fills existing rows with Akili, and new rows get it from
// SiteSettings.Create.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Infrastructure;

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
    builder.Property(settings => settings.EntraTenantAcceptanceMode)
      .IsRequired()
      .HasDefaultValue(EntraTenantAcceptanceMode.Restricted);
    builder.Property(settings => settings.EntraTenantAcceptanceInitialized)
      .IsRequired()
      .HasDefaultValue(false);
    builder.Property(settings => settings.EntraAcceptedTenants)
      .HasColumnType("jsonb")
      .IsRequired()
      .HasDefaultValueSql("'[]'::jsonb")
      .HasConversion(AcceptedTenantConverter, AcceptedTenantComparer);
    builder.Property(settings => settings.AssistantName)
      .IsRequired()
      .HasMaxLength(AssistantNameRules.MaxLength);

    builder.Property(settings => settings.Version)
      .IsConcurrencyToken()
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }

  private static readonly ValueConverter<IReadOnlyList<EntraAcceptedTenant>, string> AcceptedTenantConverter = new(
    tenants => EntraTenantIds.ToJson(tenants),
    json => EntraTenantIds.FromJson(json));

  private static readonly ValueComparer<IReadOnlyList<EntraAcceptedTenant>> AcceptedTenantComparer = new(
    (left, right) => TenantsEqual(left, right),
    tenants => TenantsHash(tenants),
    tenants => tenants.ToArray());

  private static bool TenantsEqual(
    IReadOnlyList<EntraAcceptedTenant>? left,
    IReadOnlyList<EntraAcceptedTenant>? right)
  {
    if (ReferenceEquals(left, right))
    {
      return true;
    }

    if (left is null || right is null || left.Count != right.Count)
    {
      return false;
    }

    for (int index = 0; index < left.Count; index++)
    {
      if (left[index] != right[index])
      {
        return false;
      }
    }

    return true;
  }

  private static int TenantsHash(IReadOnlyList<EntraAcceptedTenant> tenants)
  {
    HashCode hash = new();
    foreach (EntraAcceptedTenant tenant in tenants)
    {
      hash.Add(tenant);
    }

    return hash.ToHashCode();
  }
}
