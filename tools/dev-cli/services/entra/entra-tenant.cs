#region Purpose
// One Azure AD tenant as seen by `dev entra`.
#endregion

#region Design
// NameResolved is false when Graph could not supply a display name or domain (GUID only).
#endregion

namespace DevCli.Services;

internal sealed record EntraTenant(
  string TenantId,
  string? DisplayName,
  string? DefaultDomain,
  bool NameResolved);
