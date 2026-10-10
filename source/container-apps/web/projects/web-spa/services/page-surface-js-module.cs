#region Purpose
// On-demand import of page-surface.js: a bounded text summary of the page body.
#endregion

#region Design
// [SideEffectService]: components do not call this (TWA0026).
// The selector is PageAgentContext.SurfaceRootSelector, the shell's main column.
// The SyncWebMcp handler, the WebMCP dispatcher, and Ask's page_context function call this.
// TrySummarizeAsync returns null for a missing document, a disconnected circuit, a test host's
// fake runtime, or an empty walk; page_context then still carries title, purpose, and route.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Components;

[SideEffectService]
internal static class PageSurfaceJsModule
{
  internal const string Specifier = "./js/features/page-surface.js";

  internal static async Task<string?> TrySummarizeAsync(IJSRuntime? jsRuntime, CancellationToken cancellationToken)
  {
    if (jsRuntime is null)
    {
      return null;
    }

    try
    {
      string? json = await SummarizeAsync(jsRuntime, cancellationToken);
      return string.IsNullOrWhiteSpace(json) ? null : json;
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      return null;
    }
  }

  internal static async Task<string?> SummarizeAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>(
        "import",
        cancellationToken,
        Specifier);
      return await module.InvokeAsync<string>(
        "summarizeJson",
        cancellationToken,
        PageAgentContext.SurfaceRootSelector);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      return null;
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
}
