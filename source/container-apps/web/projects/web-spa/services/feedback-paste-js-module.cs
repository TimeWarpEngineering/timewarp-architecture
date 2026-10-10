#region Purpose
// On-demand IJSRuntime import of feedback-paste.js for the details box.
#endregion

#region Design
// Same import-a-named-export shape as CommandPaletteJsModule. Register binds paste on the
// details host the page passes. That host is the fluent-textarea or a wrapper around it.
// The byte limit comes from FeedbackAttachmentRules so the browser refuses an oversize
// image before reading it. The page keeps the handle and disposes it with the page.
// [SideEffectService]: the only caller is FeedbackListPage, which opts out with
// [DirectComponentSideEffect]. The upload itself is a state action.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class FeedbackPasteJsModule
{
  internal const string Specifier = "./js/features/feedback-paste.js";
  internal const string RegisterExport = "Register";
  internal const string DisposeExport = "Dispose";

  internal static async Task<IJSObjectReference> RegisterAsync<THost>(
    IJSRuntime jsRuntime,
    DotNetObjectReference<THost> host,
    ElementReference detailsHost,
    int maxBytes,
    CancellationToken cancellationToken) where THost : class
  {
    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Specifier);
      return await module.InvokeAsync<IJSObjectReference>(
        RegisterExport,
        cancellationToken,
        host,
        detailsHost,
        maxBytes);
    }
    catch (JSDisconnectedException exception)
    {
      throw new InvalidOperationException(
        "The browser circuit disconnected before feedback paste could register.",
        exception);
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
