#region Purpose
// On-demand IJSRuntime import of drawer.js: open and close one modal dialog.
#endregion

#region Design
// Same import-a-named-export shape as CommandPaletteJsModule. Bind attaches cancel and
// backdrop listeners and returns a handle. The component keeps the handle, calls Show when
// IsOpen becomes true and Close when it becomes false, and disposes the handle with the
// component. The module reference is released after Bind; the handle object stays.
// [SideEffectService]: Drawer is the caller and opts out with [DirectComponentSideEffect].
// showModal, close, and the listeners are presentational. The page's OnClose callback is
// what dispatches the state action.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class DrawerJsModule
{
  internal const string Specifier = "./js/features/drawer.js";
  internal const string BindExport = "Bind";
  internal const string ShowExport = "Show";
  internal const string CloseExport = "Close";
  internal const string DisposeExport = "Dispose";

  internal static async Task<IJSObjectReference> BindAsync<THost>
  (
    IJSRuntime jsRuntime,
    ElementReference dialog,
    DotNetObjectReference<THost> host
  ) where THost : class
  {
    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", CancellationToken.None, Specifier);
      return await module.InvokeAsync<IJSObjectReference>(BindExport, CancellationToken.None, dialog, host);
    }
    catch (JSDisconnectedException exception)
    {
      throw new InvalidOperationException("The browser circuit disconnected before the drawer could bind.", exception);
    }
    finally
    {
      if (module is not null)
      {
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
  }

  internal static async Task ShowAsync(IJSObjectReference handle)
  {
    try
    {
      await handle.InvokeVoidAsync(ShowExport);
    }
    catch (JSDisconnectedException)
    {
      // The circuit is already gone.
    }
  }

  internal static async Task CloseAsync(IJSObjectReference handle)
  {
    try
    {
      await handle.InvokeVoidAsync(CloseExport);
    }
    catch (JSDisconnectedException)
    {
      // The circuit is already gone.
    }
  }

  internal static async Task DisposeAsync(IJSObjectReference handle)
  {
    try
    {
      await handle.InvokeVoidAsync(DisposeExport);
      await handle.DisposeAsync();
    }
    catch (JSDisconnectedException)
    {
      // Server: the circuit is gone, and the listeners with it.
    }
  }
}
