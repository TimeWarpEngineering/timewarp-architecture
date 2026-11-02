#region Purpose
// Test helper that approves agent scopes on a principal before token issuance.
#endregion

#region Design
// Task 466: issuance accepts only identity:read until an admin approves more. Existing
// agent-token tests name feedback, credential, and demo scopes, so they grant those on the
// principal through this helper before they ask. identity:read is always approved and is
// not stored. The helper merges with grants already stored so a second call does not drop
// the first. It opens a scope because the postgres store is scoped.
#endregion

namespace TimeWarp.Architecture.Testing;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Identity;

/// <summary>Grants agent scopes on a principal for tests that issue a token.</summary>
public static class AgentScopeApprovals
{
  public static async Task ApproveAsync(
    IServiceProvider services,
    PrincipalId principalId,
    IEnumerable<string> scopes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(scopes);

    string[] grants = scopes
      .Where(scope => !string.Equals(scope, AgentScopes.IdentityRead, StringComparison.Ordinal))
      .Distinct(StringComparer.Ordinal)
      .ToArray();
    if (grants.Length == 0)
    {
      return;
    }

    using IServiceScope scope = services.CreateScope();
    IPrincipalAgentScopeStore store = scope.ServiceProvider.GetRequiredService<IPrincipalAgentScopeStore>();
    IReadOnlyList<string> existing = await store.GetScopesAsync(principalId, cancellationToken).ConfigureAwait(false);
    string[] merged = existing.Concat(grants).Distinct(StringComparer.Ordinal).ToArray();
    await store.SetScopesAsync(principalId, merged, cancellationToken).ConfigureAwait(false);
  }
}
