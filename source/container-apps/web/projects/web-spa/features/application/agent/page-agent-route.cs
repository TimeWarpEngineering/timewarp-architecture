#region Purpose
// The route the shell last observed for this circuit, so page tools agree with the page on screen.
#endregion

#region Design
// Scoped, not a process singleton: an InteractiveServer circuit must not read another circuit's path.
// The shell (WebMcpAgentSurface, and AgentAsk while it is open) calls Observe from its own
// NavigationManager. On WebAssembly the runtime is in-process, so Observe also reads
// window.timeWarpPagePath (pathname, search, and hash). A manager left at the base URI then
// loses to the browser URL. InteractiveServer's runtime is not in-process, so the circuit's
// NavigationManager stays the source. Callers that have no observed route, including a host
// that never registered this type, keep PageAgentScope.FromNavigation.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Route last published by the shell for the current circuit.</summary>
public sealed class PageAgentRoute
{
  public bool Observed { get; private set; }

  public string Path { get; private set; } = "/";

  public void Observe(string? path)
  {
    Path = PageAgentScope.Normalize(path);
    Observed = true;
  }

  public void Observe(NavigationManager navigation, IJSRuntime? jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(navigation);
    if (jsRuntime is IJSInProcessRuntime inProcess)
    {
      try
      {
        string? browserPath = inProcess.Invoke<string>("timeWarpPagePath");
        if (!string.IsNullOrWhiteSpace(browserPath))
        {
          Observe(browserPath);
          return;
        }
      }
      catch (Exception exception) when
      (
        exception is JSException or InvalidOperationException or ArgumentException
      )
      {
        // The page script is not on the document yet. NavigationManager is the fallback.
      }
    }

    Observe(PageAgentScope.FromNavigation(navigation));
  }

  public string PathOr(NavigationManager navigation) =>
    Observed ? Path : PageAgentScope.FromNavigation(navigation);

  public static string Current(IServiceProvider services, NavigationManager navigation)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(navigation);
    PageAgentRoute? route = services.GetService<PageAgentRoute>();
    if (route is { Observed: true })
    {
      return route.Path;
    }

    return PageAgentScope.FromNavigation(navigation);
  }
}
