#region Purpose
// App-registration lookup names and the sign-in/audience text printed by `dev entra setup`.
#endregion

#region Design
// AppLookupNames suffixes the default display name with the tenant domain and keeps the bare
// legacy name for reuse. Default display name lives on EntraSetup.
#endregion

namespace DevCli.Services;

internal static class EntraAppLookup
{
  internal static string DefaultAppDisplayName(string? tenantDomain)
  {
    if (string.IsNullOrWhiteSpace(tenantDomain))
    {
      return EntraSetup.DefaultDisplayName;
    }

    return $"{EntraSetup.DefaultDisplayName} ({tenantDomain.Trim()})";
  }

  internal static IReadOnlyList<string> AppLookupNames(string? explicitName, string? tenantDomain)
  {
    if (!string.IsNullOrWhiteSpace(explicitName))
    {
      return [explicitName.Trim()];
    }

    string preferred = DefaultAppDisplayName(tenantDomain);
    if (string.Equals(preferred, EntraSetup.DefaultDisplayName, StringComparison.Ordinal))
    {
      return [EntraSetup.DefaultDisplayName];
    }

    return [preferred, EntraSetup.DefaultDisplayName];
  }

  internal static void ResolveExistingApp(
    IReadOnlyList<string> preferredIds,
    IReadOnlyList<string> legacyIds,
    out string? appId,
    out bool ambiguous)
  {
    if (preferredIds.Count == 1)
    {
      appId = preferredIds[0];
      ambiguous = false;
      return;
    }

    if (preferredIds.Count > 1)
    {
      appId = null;
      ambiguous = true;
      return;
    }

    if (legacyIds.Count == 1)
    {
      appId = legacyIds[0];
      ambiguous = false;
      return;
    }

    if (legacyIds.Count > 1)
    {
      appId = null;
      ambiguous = true;
      return;
    }

    appId = null;
    ambiguous = false;
  }

  internal static bool PublicOriginMissingFromRedirectUris(
    string? publicOrigin,
    IReadOnlyList<string> redirectUris)
  {
    if (string.IsNullOrWhiteSpace(publicOrigin))
    {
      return false;
    }

    string origin = publicOrigin.Trim().TrimEnd('/');
    foreach (string uri in redirectUris)
    {
      if (uri.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }
    }

    return true;
  }

  internal static string SignInAudienceSummary(string? tenantDomain)
  {
    if (!string.IsNullOrWhiteSpace(tenantDomain))
    {
      return $"Sign in with an @{tenantDomain.Trim()} account (single-tenant app). Other tenants' accounts must be invited as guests first.";
    }

    return "Sign in with an account from this tenant (single-tenant app). Other tenants' accounts must be invited as guests first.";
  }

  /// <summary>
  /// Printed by <c>dev entra setup</c>. The created app is AzureADMyOrg; accepting other tenants
  /// is an admin SiteSettings policy plus a multi-tenant app registration and authority.
  /// </summary>
  internal static string MultiTenantAcceptanceNote() =>
    "Tenant acceptance is set on /Admin/Authentication (a restricted list, or any tenant), not by Authentication:Entra:TenantId. "
    + "This app is AzureADMyOrg, so only the home tenant can finish sign-in. "
    + "To accept other tenants, set supported account types to AzureADMultipleOrgs (work and school) or AzureADandPersonalMicrosoftAccount (also personal Microsoft accounts) and set Authentication:Entra:TenantId to organizations or common. "
    + "Personal accounts use tenant 9188040d-6c67-4c5b-b112-36a304b66dad and reach the app only when the authority is common or consumers. "
    + "Any tenant means any account that can finish OpenID Connect against this app can sign up or link, subject to the bootstrap and link toggles. There is no tenant blocklist.";
}
