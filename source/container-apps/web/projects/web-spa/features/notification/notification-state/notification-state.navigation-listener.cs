#region Purpose
// Scoped service that clears the shell notification region when the route changes.
#endregion

#region Design
// Rule 4 (errors clear on navigation) needs a subscriber that outlives any one page: the
// MessageBars host is re-created per page, so it cannot own the subscription. This listener
// is resolved once per circuit/scope from Routes.razor (@inject activates it) and hooks
// NavigationManager.LocationChanged — the event RouteState.ChangeRoute drives and NavLink
// navigation raises — then dispatches ClearOnNavigation through the generated ActionSet
// method (TWA0022: no direct Send). LastDispatch exposes the in-flight clear so headless
// tests and diagnostics can await it instead of polling; it is not a synchronization API
// for components.
#endregion

namespace TimeWarp.Architecture.Features;

using Microsoft.AspNetCore.Components.Routing;

partial class NotificationState
{
  public sealed class NavigationListener : IDisposable
  {
    private readonly NavigationManager NavigationManager;
    private readonly IStore Store;

    public NavigationListener(NavigationManager navigationManager, IStore store)
    {
      NavigationManager = navigationManager;
      Store = store;
      NavigationManager.LocationChanged += HandleLocationChanged;
    }

    /// <summary>The most recent ClearOnNavigation dispatch, for tests and diagnostics.</summary>
    public Task LastDispatch { get; private set; } = Task.CompletedTask;

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
      _ = sender;
      _ = args;
      LastDispatch = Store.GetState<NotificationState>().ClearOnNavigation();
    }

    public void Dispose()
    {
      NavigationManager.LocationChanged -= HandleLocationChanged;
    }
  }
}
