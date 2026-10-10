#region Purpose
// The route the shell last observed for this circuit, so page tools agree with the page on screen.
#endregion

#region Design
// Scoped, not a process singleton: an InteractiveServer circuit must not read another circuit's path.
// The shell (WebMcpAgentSurface, and AgentAsk while it is open) calls Observe from its own
// NavigationManager. On WebAssembly the runtime is in-process, so Observe also reads
// window.timeWarpPagePath (base-relative pathname, search, and hash). A manager left at the base
// URI then loses to the browser URL. InteractiveServer's runtime is not in-process, so the
// circuit's NavigationManager stays the source. Callers that have no observed route, including a
// host that never registered this type, keep PageAgentScope.FromNavigation.
// Reads are live, not a snapshot. Focused pages host neither observer, so a stored path would go
// stale on a hop between two of them, and the WebMCP dispatcher re-selects tools for the current
// route because the browser agent is not trusted. PathOr therefore reads the browser path again
// when Observe found one. Otherwise it returns the live path of the caller's manager when that is
// the manager Observe read. The stored path is only for a caller holding a different manager,
// such as a function provider whose manager never left the base URI.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Route last published by the shell for the current circuit.</summary>
public sealed class PageAgentRoute
{
  private IJSInProcessRuntime? Browser;
  private NavigationManager? Source;

  public bool Observed { get; private set; }

  public string Path { get; private set; } = "/";

  public void Observe(string? path)
  {
    Path = PageAgentScope.Normalize(path);
    Observed = true;
    Browser = null;
    Source = null;
  }

  public void Observe(NavigationManager navigation, IJSRuntime? jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(navigation);
    if (jsRuntime is IJSInProcessRuntime inProcess && TryReadBrowser(inProcess, out string browserPath))
    {
      Observe(browserPath);
      Browser = inProcess;
      return;
    }

    Observe(PageAgentScope.FromNavigation(navigation));
    Source = navigation;
  }

  public string PathOr(NavigationManager navigation)
  {
    ArgumentNullException.ThrowIfNull(navigation);
    if (!Observed)
    {
      return PageAgentScope.FromNavigation(navigation);
    }

    if (Browser is not null && TryReadBrowser(Browser, out string browserPath))
    {
      Path = PageAgentScope.Normalize(browserPath);
      return Path;
    }

    if (ReferenceEquals(Source, navigation))
    {
      Path = PageAgentScope.FromNavigation(navigation);
    }

    return Path;
  }

  public static string Current(IServiceProvider services, NavigationManager navigation)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(navigation);
    PageAgentRoute? route = services.GetService<PageAgentRoute>();
    return route is null ? PageAgentScope.FromNavigation(navigation) : route.PathOr(navigation);
  }

  private static bool TryReadBrowser(IJSInProcessRuntime browser, out string path)
  {
    try
    {
      string? browserPath = browser.Invoke<string>("timeWarpPagePath");
      if (!string.IsNullOrWhiteSpace(browserPath))
      {
        path = browserPath;
        return true;
      }
    }
    catch (Exception exception) when
    (
      exception is JSException or InvalidOperationException or ArgumentException
    )
    {
      // The page script is not on the document yet. NavigationManager is the fallback.
    }

    path = "/";
    return false;
  }
}
