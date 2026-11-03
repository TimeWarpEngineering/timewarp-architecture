#region Purpose
// Redirect-URI building, union, and comparison for the Entra app registration.
#endregion

#region Design
// Redirect-URI union is case-insensitive and preserves first-seen order (existing, then desired).
// The fixed local URIs and callback path are constants on EntraSetup.
#endregion

namespace DevCli.Services;

internal static class EntraRedirectUris
{
  internal static IReadOnlyList<string> BuildDesiredRedirectUris(
    string? publicOrigin,
    IReadOnlyList<string>? extraRedirectUris)
  {
    List<string> desired = [EntraSetup.WebServerHttpsRedirect, EntraSetup.IngressHttpsRedirect];
    if (!string.IsNullOrWhiteSpace(publicOrigin))
    {
      desired.Add(ToSignInOidcUri(publicOrigin));
    }

    if (extraRedirectUris is not null)
    {
      desired.AddRange(extraRedirectUris);
    }

    return UnionRedirectUris([], desired);
  }

  internal static string ToSignInOidcUri(string origin)
  {
    string trimmed = origin.Trim().TrimEnd('/');
    if (trimmed.EndsWith(EntraSetup.CallbackPath, StringComparison.OrdinalIgnoreCase))
    {
      return trimmed;
    }

    return trimmed + EntraSetup.CallbackPath;
  }

  internal static IReadOnlyList<string> UnionRedirectUris(
    IEnumerable<string> existing,
    IEnumerable<string> desired)
  {
    List<string> union = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    foreach (string uri in existing.Concat(desired))
    {
      string trimmed = uri.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      if (seen.Add(trimmed))
      {
        union.Add(trimmed);
      }
    }

    return union;
  }

  internal static bool RedirectUrisEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
  {
    if (left.Count != right.Count)
    {
      return false;
    }

    HashSet<string> leftSet = new(left, StringComparer.OrdinalIgnoreCase);
    foreach (string uri in right)
    {
      if (!leftSet.Contains(uri))
      {
        return false;
      }
    }

    return true;
  }
}
