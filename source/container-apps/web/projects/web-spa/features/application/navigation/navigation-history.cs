#region Purpose
// The absolute addresses this circuit has shown, newest first, recorded as each navigation happens.
#endregion

#region Design
// Task 521 (review round 2, M1). RouteState.Routes is a back stack, not a visit list:
// TwPageTitle's PushRouteInfo pops every entry down to an earlier copy of the same URL, so a
// second visit to a page erases the pages between the two visits. A filing that needs "the page
// the filer just left" reads this list instead. Nothing here removes an entry except the cap.
// Scoped, one per circuit (WebAssembly: one per app). Routes.razor's @inject activates it, like
// NotificationState.NavigationListener, so it is listening before the first link click. The
// constructor records the address the circuit started on, because a direct load raises no
// LocationChanged. A repeat of the newest address (a same-page navigation) is not added again.
// The list keeps MaxVisits entries and drops the oldest, so a consumer that skips some addresses
// finds no match rather than an older one. Addresses are kept as given (query and fragment
// included); a consumer that sends one anywhere must cut what it does not need.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.Routing;

/// <summary>Visited absolute addresses for this circuit, newest first.</summary>
public sealed class NavigationHistory : IDisposable
{
  /// <summary>The number of addresses kept; the oldest is dropped past this.</summary>
  public const int MaxVisits = 64;

  private readonly NavigationManager NavigationManager;
  private readonly LinkedList<string> Entries = new();

  public NavigationHistory(NavigationManager navigationManager)
  {
    ArgumentNullException.ThrowIfNull(navigationManager);
    NavigationManager = navigationManager;
    Record(NavigationManager.Uri);
    NavigationManager.LocationChanged += HandleLocationChanged;
  }

  /// <summary>Absolute addresses, newest first.</summary>
  public IEnumerable<string> Visits => Entries;

  public void Dispose()
  {
    NavigationManager.LocationChanged -= HandleLocationChanged;
  }

  private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
  {
    _ = sender;
    Record(args.Location);
  }

  private void Record(string address)
  {
    if (string.Equals(Entries.First?.Value, address, StringComparison.Ordinal))
    {
      return;
    }

    Entries.AddFirst(address);
    if (Entries.Count > MaxVisits)
    {
      Entries.RemoveLast();
    }
  }
}
