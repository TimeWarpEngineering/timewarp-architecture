#region Purpose
// Names which catalog actions are bound to the page on screen, and which page executes them.
#endregion

#region Design
// Derived, not hand-written (task 300). A page declares the actions its own controls dispatch
// with [Page(AgentActions = [typeof(...Action)])], and the [Page] generator emits every such page
// as a PageRegistry.ActionScopes row with all its route templates (primary and aliases,
// parameterized or not). This type matches the current path against those templates; it holds no
// route or page list of its own, and the guard test fails if one reappears under agent/.
// A template matches segment by segment, case-insensitive. A {Name} token matches one non-empty
// segment, and a {Name:guid|int|long|bool} token only a value of that type, so /Feedback/{id:Guid}
// serves the Feedback actions on a filing and /Feedback/item or /FeedbackExtra serves none.
// PrimaryPage is the static route of the first scope that lists the action, taking navigable
// pages in menu order (PageRegistry.All) first, so FetchCredentials executes on Settings before
// Passkeys. Serves is true when the action is not page-bound, or a scope listing it matches path.
// CatalogAgentToolSet uses this to decide execution versus a navigate offer. It is not the agent
// tool list: palette commands whose visibility includes Agent are offered on every page.
// Human-only actions may be listed; the visibility filter, not this type, drops them.
// StateTypesFor gives the states that own the page's actions; PageAgentContext asks each one that
// implements IAgentPageFacts for its facts, so page_context has no per-route cases either.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Page-bound agent actions, from the generated <see cref="PageRegistry.ActionScopes"/>.</summary>
public static class PageAgentScope
{
  /// <summary>Static routes of pages that declare agent actions.</summary>
  public static IReadOnlyCollection<string> KnownRoutes
  {
    get
    {
      List<string> routes = [];
      foreach (PageActionScope scope in PageRegistry.ActionScopes)
      {
        foreach (string template in scope.RouteTemplates)
        {
          if (!template.Contains('{', StringComparison.Ordinal))
          {
            routes.Add(template);
          }
        }
      }

      return routes;
    }
  }

  /// <summary>True when some page declares <paramref name="actionType"/>.</summary>
  public static bool IsPageBound(Type actionType) => PrimaryScope(actionType, requireStatic: false) is not null;

  /// <summary>The static route that executes <paramref name="actionType"/>, or null when it is not page-bound.</summary>
  public static string? PrimaryPage(Type actionType)
  {
    PageActionScope? scope = PrimaryScope(actionType, requireStatic: true);
    return scope is null ? null : StaticRoute(scope);
  }

  /// <summary>True when the action may run on <paramref name="path"/>.</summary>
  public static bool Serves(string? path, Type actionType)
  {
    ArgumentNullException.ThrowIfNull(actionType);
    if (!IsPageBound(actionType))
    {
      return true;
    }

    string normalized = Normalize(path);
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      if (scope.ActionTypes.Contains(actionType) && MatchesAny(scope, normalized))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Action types the page on <paramref name="path"/> declares, in declaration order.</summary>
  public static IReadOnlyList<Type> ActionTypesFor(string? path)
  {
    string normalized = Normalize(path);
    List<Type> types = [];
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      if (!MatchesAny(scope, normalized))
      {
        continue;
      }

      foreach (Type type in scope.ActionTypes)
      {
        if (!types.Contains(type))
        {
          types.Add(type);
        }
      }
    }

    return types;
  }

  /// <summary>The state types that own the page's actions on <paramref name="path"/>.</summary>
  public static IReadOnlyList<Type> StateTypesFor(string? path)
  {
    List<Type> states = [];
    foreach (Type action in ActionTypesFor(path))
    {
      for (Type? owner = action.DeclaringType; owner is not null; owner = owner.DeclaringType)
      {
        if (typeof(IState).IsAssignableFrom(owner))
        {
          if (!states.Contains(owner))
          {
            states.Add(owner);
          }

          break;
        }
      }
    }

    return states;
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

  /// <summary>True when <paramref name="template"/> (a [Page] route template) matches the normalized <paramref name="path"/>.</summary>
  public static bool Matches(string template, string path)
  {
    ArgumentNullException.ThrowIfNull(template);
    string[] expected = Normalize(template).Split('/');
    string[] actual = Normalize(path).Split('/');
    if (expected.Length != actual.Length)
    {
      return false;
    }

    for (int index = 0; index < expected.Length; index++)
    {
      string segment = expected[index];
      bool token = segment.StartsWith('{', StringComparison.Ordinal) && segment.EndsWith('}', StringComparison.Ordinal);
      if (token ? !TokenAccepts(segment, actual[index]) : !string.Equals(segment, actual[index], StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }
    }

    return true;
  }

  private static bool TokenAccepts(string token, string value)
  {
    if (value.Length == 0)
    {
      return false;
    }

    int colon = token.IndexOf(':', StringComparison.Ordinal);
    string constraint = colon < 0 ? "" : token[(colon + 1)..^1].Trim().ToLowerInvariant();
    return constraint switch
    {
      "guid" => Guid.TryParse(value, out _),
      "int" => int.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _),
      "long" => long.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _),
      "bool" => bool.TryParse(value, out _),
      _ => true,
    };
  }

  private static bool MatchesAny(PageActionScope scope, string normalizedPath)
  {
    foreach (string template in scope.RouteTemplates)
    {
      if (Matches(template, normalizedPath))
      {
        return true;
      }
    }

    return false;
  }

  private static string? StaticRoute(PageActionScope scope)
  {
    foreach (string template in scope.RouteTemplates)
    {
      if (!template.Contains('{', StringComparison.Ordinal))
      {
        return Normalize(template);
      }
    }

    return null;
  }

  private static PageActionScope? PrimaryScope(Type actionType, bool requireStatic)
  {
    ArgumentNullException.ThrowIfNull(actionType);
    PageActionScope? fallback = null;
    int best = int.MaxValue;
    PageActionScope? chosen = null;
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      if (!scope.ActionTypes.Contains(actionType) || (requireStatic && StaticRoute(scope) is null))
      {
        continue;
      }

      int rank = MenuRank(scope.PageType);
      if (rank < best)
      {
        best = rank;
        chosen = scope;
      }

      fallback ??= scope;
    }

    return chosen ?? fallback;
  }

  private static int MenuRank(Type pageType)
  {
    for (int index = 0; index < PageRegistry.All.Count; index++)
    {
      if (PageRegistry.All[index].PageType == pageType)
      {
        return index;
      }
    }

    return int.MaxValue - 1;
  }
}
