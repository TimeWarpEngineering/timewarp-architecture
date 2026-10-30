#region Purpose
// Host-supplied agent-bearer scheme name and policy-to-scope map for the OpenAPI document.
#endregion

#region Design
// foundation-server cannot reference PermissionIds, AgentScopes, or AgentTokenDefaults.
// Each host passes the map it already uses to authorize: web inverts
// AgentScopePermissionSeed.DefaultGrants (scope → permissions becomes permission → scope);
// api inverts the same pairs it registers with AddPolicy. A permission granted by two
// different scopes is rejected here, because one OpenAPI security requirement treats every
// listed scope as required. The host decides that case instead of this package picking one.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Agent bearer facts the host already owns. Foundation does not store product policy or scope strings.
/// </summary>
public sealed class OpenApiAgentBearer
{
  /// <summary>ASP.NET scheme name the endpoint lists, such as the host's agent-token constant.</summary>
  public string SchemeName { get; }

  /// <summary>Policy name → scope string required on the bearer.</summary>
  public IReadOnlyDictionary<string, string> PolicyScopes { get; }

  /// <summary>
  /// Copies <paramref name="policyScopes"/> with an ordinal comparer.
  /// </summary>
  public OpenApiAgentBearer(string schemeName, IReadOnlyDictionary<string, string> policyScopes)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);
    ArgumentNullException.ThrowIfNull(policyScopes);
    SchemeName = schemeName;
    Dictionary<string, string> copy = new(StringComparer.Ordinal);
    foreach ((string policy, string scope) in policyScopes)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(policy);
      ArgumentException.ThrowIfNullOrWhiteSpace(scope);
      copy[policy] = scope;
    }

    PolicyScopes = copy;
  }

  /// <summary>
  /// Invert scope → permission (or policy) ids into policy → scope.
  /// </summary>
  /// <exception cref="InvalidOperationException">A permission id is granted by two different scopes.</exception>
  public static Dictionary<string, string> PolicyScopesFromGrants
  (
    IReadOnlyDictionary<string, IReadOnlyList<string>> scopeGrants
  )
  {
    ArgumentNullException.ThrowIfNull(scopeGrants);
    Dictionary<string, string> policyScopes = new(StringComparer.Ordinal);
    foreach ((string scope, IReadOnlyList<string> permissions) in scopeGrants)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(scope);
      if (permissions is null)
      {
        continue;
      }

      foreach (string permission in permissions)
      {
        if (string.IsNullOrWhiteSpace(permission))
        {
          continue;
        }

        if (policyScopes.TryGetValue(permission, out string? existing)
          && !string.Equals(existing, scope, StringComparison.Ordinal))
        {
          throw new InvalidOperationException
          (
            $"Permission '{permission}' is granted by both '{existing}' and '{scope}'. "
            + "One OpenAPI security requirement cannot document that as a single required scope."
          );
        }

        policyScopes[permission] = scope;
      }
    }

    return policyScopes;
  }
}
