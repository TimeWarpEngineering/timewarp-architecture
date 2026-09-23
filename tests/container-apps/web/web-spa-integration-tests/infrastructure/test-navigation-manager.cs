#region Purpose
// Headless NavigationManager for the SPA test host: NavigateTo updates Uri and raises LocationChanged.
#endregion

#region Design
// The SPA host has no renderer, so Blazor's WebAssembly/remote NavigationManager is unavailable.
// RouteState.ChangeRoute's handler and NotificationState.NavigationListener both take
// NavigationManager; this stub lets a test drive a real route change through the state
// pipeline and observe LocationChanged subscribers (task 247 navigation-clears-errors case).
// Registered scoped so the handler and the listener share one instance per SpaTestScope.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using Microsoft.AspNetCore.Components;

public sealed class TestNavigationManager : NavigationManager
{
  public TestNavigationManager()
  {
    Initialize("http://localhost/", "http://localhost/");
  }

  protected override void NavigateToCore(string uri, NavigationOptions options)
  {
    Uri = ToAbsoluteUri(uri).ToString();
    NotifyLocationChanged(isInterceptedLink: false);
  }
}
