#region Purpose
// Runtime choice for which Entra tenants may sign in: a restricted id list, or any tenant.
#endregion

#region Design
// Stored on SiteSettings and edited on /Admin/Authentication. Restricted is the default so an
// uninitialized row refuses every tenant until an administrator lists one or switches to Any.
// Any accepts every non-empty tid. Personal Microsoft accounts are not a third mode: they arrive
// only when the OIDC authority is common or consumers, and their tid is EntraTenantIds.Consumers.
// There is no blocklist.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Which Entra tenant ids may complete sign-in, link, and bootstrap.
/// </summary>
public enum EntraTenantAcceptanceMode
{
  /// <summary>Accept only tenant ids in the site-settings list. An empty list accepts none.</summary>
  Restricted = 0,

  /// <summary>Accept any non-empty tenant id. The issuer is still pinned to that id.</summary>
  Any = 1
}
