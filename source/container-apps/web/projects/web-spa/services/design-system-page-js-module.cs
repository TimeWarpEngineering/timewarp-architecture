#region Purpose
// On-demand import of design-system-page.js: scroll the shell content to the top,
// and read the resolved color of design-system token chips.
#endregion

#region Design
// [SideEffectService]: DesignSystemPage and ColorTokensSample are the callers and opt
// out with [DirectComponentSideEffect]. Scroll position and computed colors are DOM
// state. TryImportAsync returns null when the runtime cannot load the module (a static
// host, or a disconnected circuit). The module stays loaded after the proxy is
// released, so a theme watch started on one proxy keeps running until stopThemeWatch.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class DesignSystemPageJsModule
{
  internal const string Specifier = "./js/features/design-system-page.js";

  internal static async Task ScrollContentToTopAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    IJSObjectReference? module = await TryImportAsync(jsRuntime);
    if (module is null)
    {
      return;
    }

    try
    {
      await module.InvokeVoidAsync("scrollContentToTop", CancellationToken.None);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      // The circuit left before the scroller could move.
    }
    finally
    {
      await ReleaseAsync(module);
    }
  }

  internal static async Task<IJSObjectReference?> TryImportAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    try
    {
      return await jsRuntime.InvokeAsync<IJSObjectReference>("import", CancellationToken.None, Specifier);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      return null;
    }
  }

  internal static async Task<string[]?> ReadTokenColorsAsync(IJSObjectReference module, string[] names)
  {
    ArgumentNullException.ThrowIfNull(module);
    ArgumentNullException.ThrowIfNull(names);
    try
    {
      // A string[] binds to params object[] as the argument list. One wrapper keeps
      // the names as a single array for readTokenColors.
      return await module.InvokeAsync<string[]>("readTokenColors", CancellationToken.None, [names]);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      return null;
    }
  }

  internal static async Task WatchThemeAsync<THost>(IJSObjectReference module, DotNetObjectReference<THost> host)
    where THost : class
  {
    ArgumentNullException.ThrowIfNull(module);
    ArgumentNullException.ThrowIfNull(host);
    try
    {
      await module.InvokeVoidAsync("watchTheme", CancellationToken.None, host);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      // The circuit left before the theme watch could start.
    }
  }

  internal static async Task StopThemeWatchAsync(IJSObjectReference module)
  {
    ArgumentNullException.ThrowIfNull(module);
    try
    {
      await module.InvokeVoidAsync("stopThemeWatch", CancellationToken.None);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      // The circuit is already gone.
    }
  }

  internal static async Task ReleaseAsync(IJSObjectReference module)
  {
    ArgumentNullException.ThrowIfNull(module);
    try
    {
      await module.DisposeAsync();
    }
    catch (JSDisconnectedException)
    {
      // The circuit is already gone.
    }
  }
}
