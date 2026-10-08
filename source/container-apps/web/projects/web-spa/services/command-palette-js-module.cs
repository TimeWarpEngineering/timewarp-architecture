#region Purpose
// On-demand IJSRuntime import of command-palette.js: registers the Ctrl-K hotkey and search-field trigger.
#endregion

#region Design
// Task 239-003: same import-a-named-export shape as SignOutJsModule / WebAuthnJsModule (no
// window.Spa dependency). Register returns the JS handle as an IJSObjectReference; the caller
// keeps it for RestoreFocus and disposes it (Dispose export, then the reference) when its
// TimeWarpPage goes away. The selectors are TriggerAttribute (the one attribute the shell puts on
// the appbar search field) and InputAttribute (the palette query box), so the TS holds no markup
// knowledge.
// [SideEffectService] (task 265): TWA0026 reports component calls. CommandPalette is the one
// caller and opts out with [DirectComponentSideEffect] — focus, scroll and hotkey wiring are
// presentational JS with no store state to dispatch.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class CommandPaletteJsModule
{
  internal const string Specifier = "./js/features/command-palette.js";
  internal const string RegisterExport = "Register";
  internal const string RestoreFocusExport = "RestoreFocus";
  internal const string ScrollIntoViewExport = "ScrollIntoView";
  internal const string DisposeExport = "Dispose";

  /// <summary>Marks the element whose focus or click opens the palette.</summary>
  internal const string TriggerAttribute = "data-command-palette-trigger";

  /// <summary>Marks the palette query input (ArrowUp/ArrowDown caret movement is suppressed on it).</summary>
  internal const string InputAttribute = "data-command-palette-input";

  internal static async Task<IJSObjectReference> RegisterAsync<THost>
  (
    IJSRuntime jsRuntime,
    DotNetObjectReference<THost> host,
    CancellationToken cancellationToken
  ) where THost : class
  {
    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Specifier);
      return await module.InvokeAsync<IJSObjectReference>(RegisterExport, cancellationToken, host, $"[{TriggerAttribute}]", $"[{InputAttribute}]");
    }
    catch (JSDisconnectedException exception)
    {
      throw new InvalidOperationException("The browser circuit disconnected before the command palette could register.", exception);
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
