#region Purpose
// Host-free helpers: configuration-tenant drift against persisted trusted tenants, and add-tenant list merge.
#endregion

#region Design
// Task 225: site settings own Entra policy after first-run seed. Configuration:Entra:TenantId can
// drift (re-run `dev entra setup` against a different tenant). Compare by Guid, not string
// casing. Non-GUID TenantId values (appsettings placeholder "organizations") are not drift —
// there is no tenant to add. WithConfigurationTenant appends D-format and de-dupes. Shared by
// the seeder (log text), Get/SPA state, and the Admin Authentication add-tenant action.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

public static class SiteSettingsConfigurationDrift
{
  public const string AdminPageRoute = "/Admin/Authentication";
  public const string Remediation = "edit on /Admin/Authentication or run `dev entra reseed`";

  public static bool TryParseTenantId(string? value, out Guid tenantId)
  {
    tenantId = Guid.Empty;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    return Guid.TryParse(value.Trim(), out tenantId) && tenantId != Guid.Empty;
  }

  public static bool IsConfigurationTenantUntrusted(
    string? configurationTenantId,
    IEnumerable<string> trustedTenants)
  {
    ArgumentNullException.ThrowIfNull(trustedTenants);
    if (!TryParseTenantId(configurationTenantId, out Guid configured))
    {
      return false;
    }

    foreach (string entry in trustedTenants)
    {
      if (TryParseTenantId(entry, out Guid trusted) && trusted == configured)
      {
        return false;
      }
    }

    return true;
  }

  public static string FormatTenantLabel(string? displayName, string? domain, string? tenantId)
  {
    string? name = NullIfWhiteSpace(displayName);
    string? tenantDomain = NullIfWhiteSpace(domain);
    if (name is not null && tenantDomain is not null)
    {
      return $"{name} ({tenantDomain})";
    }

    if (name is not null)
    {
      return name;
    }

    if (tenantDomain is not null)
    {
      return tenantDomain;
    }

    if (TryParseTenantId(tenantId, out Guid parsed))
    {
      return parsed.ToString("D");
    }

    string? raw = NullIfWhiteSpace(tenantId);
    return raw ?? "(not set)";
  }

  public static List<string> WithConfigurationTenant(
    IReadOnlyList<string> trustedTenants,
    string configurationTenantId)
  {
    ArgumentNullException.ThrowIfNull(trustedTenants);
    ArgumentException.ThrowIfNullOrWhiteSpace(configurationTenantId);

    List<string> result = [];
    HashSet<Guid> seen = [];
    foreach (string entry in trustedTenants)
    {
      if (TryParseTenantId(entry, out Guid trusted) && seen.Add(trusted))
      {
        result.Add(trusted.ToString("D"));
      }
    }

    if (TryParseTenantId(configurationTenantId, out Guid configured) && seen.Add(configured))
    {
      result.Add(configured.ToString("D"));
    }

    return result;
  }

  private static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
