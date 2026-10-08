#region Purpose
// Names the catalog actions each page's buttons dispatch, so agents see that page and no other.
#endregion

#region Design
// Task 282: tools are the actions the current page dispatches, not the whole catalog. The lists
// are the button call sites (Settings, Passkeys, Profile, Counter, new role, authentication
// settings). Human-only entries stay in the list so the visibility filter, not a second copy of
// the page, is what drops them. Paths match [Page] routes, ordinal and case-insensitive, with
// the query and hash removed. A route with no entry offers no catalog tools.
// Profile.UpdateProfile and SiteSettings.UpdateSiteSettings replace whole records (the latter
// with a Version token), so page_context on those routes carries the current values the agent
// must echo (PageAgentContext); there is no separate read tool on those pages.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class PageAgentScope
{
  private static readonly Dictionary<string, string[]> Actions = new(StringComparer.OrdinalIgnoreCase)
  {
    ["/Counter"] = ["Counter.IncrementCounter"],
    ["/Settings"] =
    [
      "Credentials.FetchCredentials",
      "Credentials.AddPasskey",
      "Credentials.AddExistingPasskey",
      "Credentials.RevokeCredential",
      "Credentials.RenameCredential",
      "Credentials.LinkMicrosoft365",
    ],
    ["/Passkeys"] =
    [
      "Credentials.FetchCredentials",
      "Credentials.RevokeCredential",
      "Credentials.RenameCredential",
    ],
    ["/Profile"] = ["Profile.UpdateProfile"],
    ["/Admin/Roles/New"] = ["Role.CreateRole"],
    ["/Admin/Authentication"] = ["SiteSettings.UpdateSiteSettings"],
  };

  public static string Normalize(string? path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return "/";
    }

    string normalized = path;
    int query = normalized.IndexOf('?', StringComparison.Ordinal);
    if (query >= 0)
    {
      normalized = normalized[..query];
    }

    int hash = normalized.IndexOf('#', StringComparison.Ordinal);
    if (hash >= 0)
    {
      normalized = normalized[..hash];
    }

    if (!normalized.StartsWith('/', StringComparison.Ordinal))
    {
      normalized = "/" + normalized;
    }

    if (normalized.Length > 1)
    {
      normalized = normalized.TrimEnd('/');
    }

    return normalized;
  }

  public static string FromNavigation(NavigationManager navigation)
  {
    ArgumentNullException.ThrowIfNull(navigation);
    string relative = navigation.ToBaseRelativePath(navigation.Uri);
    return Normalize(string.IsNullOrEmpty(relative) ? "/" : relative);
  }

  public static IReadOnlyList<string> ActionNamesFor(string? path)
  {
    string normalized = Normalize(path);
    return Actions.TryGetValue(normalized, out string[]? names) ? names : [];
  }
}
