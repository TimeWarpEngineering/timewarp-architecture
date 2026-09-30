#region Purpose
// On-demand IJSRuntime import of command-palette.js: registers the Ctrl-K hotkey and search-field trigger.
#endregion

#region Design
// Task 239-003: same import-a-named-export shape as SignOutJsModule / WebAuthnJsModule (no
// window.Spa dependency). Register returns the JS handle as an IJSObjectReference; the caller
// keeps it for RestoreFocus and disposes it (Dispose export, then the reference) when its
// TimeWarpPage goes away. The trigger selector is TriggerAttribute, the one attribute the shell
// puts on the appbar search field, so the TS holds no markup knowledge.
#endregion

namespace TimeWarp.Architecture.Services;

internal static class CommandPaletteJsModule
{
  internal const string Specifier = "./js/features/command-palette.js";
  internal const string RegisterExport = "Register";
  internal const string RestoreFocusExport = "RestoreFocus";
  internal const string DisposeExport = "Dispose";

  /// <summary>Marks the element whose focus or click opens the palette.</summary>
  internal const string TriggerAttribute = "data-command-palette-trigger";

  internal static async Task<IJSObjectReference> RegisterAsync<THost>
  (
    IJSRuntime jsRuntime,
    DotNetObjectReference<THost> host,
    CancellationToken cancellationToken
  ) where THost : class
  {
    IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Specifier);
    try
    {
      return await module.InvokeAsync<IJSObjectReference>(RegisterExport, cancellationToken, host, $"[{TriggerAttribute}]");
    }
    finally
    {
      await module.DisposeAsync();
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
