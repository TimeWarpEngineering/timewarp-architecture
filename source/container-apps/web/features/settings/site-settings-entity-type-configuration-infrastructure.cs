#region Purpose
// EF Core mapping for TimeWarp.Identity SiteSettings: schema identity, singleton row, store-CAS Version.
#endregion

#region Design
// Table identity.site_settings — alongside identity.principals, never inside principals.
// SiteSettings is port-backed (ISiteSettingsStore), not IAggregateRoot; Version is store-CAS plus
// .IsConcurrencyToken() like Principal. Trusted tenants persist as jsonb GUID strings so the
// column stays a single value (no extra table). Slice placement: mapping lives under
// features/settings/*-infrastructure.cs (settings product slice) even though the CLR type is
// TimeWarp.Identity — TWA0009 does not apply across assemblies.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Infrastructure;

using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TimeWarp.Identity;

public sealed class SiteSettingsEntityTypeConfiguration : IEntityTypeConfiguration<SiteSettings>
{
  public const string SchemaName = "identity";
  public const string TableName = "site_settings";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    builder.Property(settings => settings.EntraTrustedTenants)
      .HasColumnType("jsonb")
      .HasConversion(
        tenants => JsonSerializer.Serialize(tenants, JsonOptions),
        json => JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? new List<Guid>())
      .Metadata.SetValueComparer(
        new ValueComparer<IReadOnlyList<Guid>>(
          (left, right) => left!.SequenceEqual(right!),
          tenants => tenants.Aggregate(0, static (hash, tenantId) => HashCode.Combine(hash, tenantId)),
          tenants => tenants.ToList()));

    builder.Property(settings => settings.Version)
      .IsConcurrencyToken()
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
