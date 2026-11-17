#region Purpose
// Outcome of choosing a tenant from the visible list.
#endregion

#region Design
// Ambiguous covers both an omitted choice with many tenants and a --tenant that matches several.
#endregion

namespace DevCli.Services;

internal enum TenantSelectionStatus
{
  Selected,
  Ambiguous,
  NoneVisible,
  NotFound
}
