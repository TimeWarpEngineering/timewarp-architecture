#region Purpose
// Result of EntraTenants.SelectTenant: status, chosen tenant, and the candidate list.
#endregion

#region Design
// Candidates are all visible tenants, or the matches when --tenant was given.
#endregion

namespace DevCli.Services;

internal sealed record TenantSelection(
  TenantSelectionStatus Status,
  EntraTenant? Selected,
  IReadOnlyList<EntraTenant> Candidates);
