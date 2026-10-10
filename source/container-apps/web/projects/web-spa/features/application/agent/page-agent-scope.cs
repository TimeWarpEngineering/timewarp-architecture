#region Purpose
// Names which catalog actions are bound to a page, and which route executes them.
#endregion

#region Design
// The map is the reverse of "which buttons this page dispatches". PrimaryPage is the first route
// in insertion order (Settings before Passkeys, so FetchCredentials executes on Settings).
// Serves is true when the action is not page-bound, or the current path is one of its routes.
// /Feedback is the one prefix: /Feedback/{id} serves the Feedback actions. The match requires a
// following slash, so /FeedbackExtra does not. Other routes stay exact.
// CatalogAgentToolSet uses this to decide execution versus a navigate offer. It is not the agent
// tool list: palette commands whose visibility includes Agent are offered on every page.
// Human-only names stay in the map so the visibility filter, not a second copy of the page, drops
// them. Profile.UpdateProfile and SiteSettings.UpdateSiteSettings replace whole records (the latter
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
    ["/Feedback"] =
    [
      "Feedback.SubmitFeedback",
      "Feedback.ListMyFeedback",
      "Feedback.OpenFeedback",
    ],
  };

  private static readonly string[] PrefixRoutes = ["/Feedback"];

  /// <summary>Routes that name page-bound catalog actions.</summary>
  public static IReadOnlyCollection<string> KnownRoutes => Actions.Keys;

  /// <summary>Routes that execute <paramref name="actionName"/>, in map insertion order.</summary>
  public static IReadOnlyList<string> PagesFor(string actionName)
  {
    List<string> pages = [];
    foreach (KeyValuePair<string, string[]> pair in Actions)
    {
      foreach (string name in pair.Value)
      {
        if (string.Equals(name, actionName, StringComparison.Ordinal))
        {
          pages.Add(pair.Key);
          break;
        }
      }
    }

    return pages;
  }

  /// <summary>The route that executes <paramref name="actionName"/>, or null when it is not page-bound.</summary>
  public static string? PrimaryPage(string actionName)
  {
    IReadOnlyList<string> pages = PagesFor(actionName);
    return pages.Count == 0 ? null : pages[0];
  }

  /// <summary>True when the action may run on <paramref name="path"/>.</summary>
  public static bool Serves(string? path, string actionName)
  {
    IReadOnlyList<string> pages = PagesFor(actionName);
    if (pages.Count == 0)
    {
      return true;
    }

    string normalized = Normalize(path);
    foreach (string page in pages)
    {
      if (string.Equals(normalized, page, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      if (IsPrefixRoute(page)
        && normalized.StartsWith(page + "/", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsPrefixRoute(string page)
  {
    foreach (string prefix in PrefixRoutes)
    {
      if (string.Equals(prefix, page, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

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
    if (Actions.TryGetValue(normalized, out string[]? names))
    {
      return names;
    }

    foreach (string prefix in PrefixRoutes)
    {
      if (normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
        && Actions.TryGetValue(prefix, out string[]? prefixed))
      {
        return prefixed;
      }
    }

    return [];
  }
}
