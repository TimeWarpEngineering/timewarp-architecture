#region Purpose
// Host-free helpers: app-registration tenant label for Admin/Authentication.
#endregion

#region Design
// Task 225 / 227: site settings own Entra offered/bootstrap after first-run seed. Trust is
// Authentication:Entra:TenantId, not a persisted allowlist, so there is no tenant-drift
// comparison and no add-tenant merge. FormatTenantLabel is the read-only "App registration
// tenant" line: `{DisplayName} ({Domain}) — {GUID}` when name and domain are known, otherwise
// the bare GUID (or the raw TenantId string when it is not a GUID).
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

  public static string FormatTenantLabel(string? displayName, string? domain, string? tenantId)
  {
    string? guidText = TryParseTenantId(tenantId, out Guid parsed) ? parsed.ToString("D") : null;
    string? name = NullIfWhiteSpace(displayName);
    string? tenantDomain = NullIfWhiteSpace(domain);

    if (name is not null && tenantDomain is not null && guidText is not null)
    {
      return $"{name} ({tenantDomain}) — {guidText}";
    }

    if (name is not null && guidText is not null)
    {
      return $"{name} — {guidText}";
    }

    if (tenantDomain is not null && guidText is not null)
    {
      return $"{tenantDomain} — {guidText}";
    }

    if (guidText is not null)
    {
      return guidText;
    }

    string? raw = NullIfWhiteSpace(tenantId);
    return raw ?? "(not set)";
  }

  private static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
