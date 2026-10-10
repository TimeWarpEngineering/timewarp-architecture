#region Purpose
// On-demand import of page-surface.js: a bounded text summary of the page body.
#endregion

#region Design
// [SideEffectService]: the SyncWebMcp handler calls this. Components do not (TWA0026).
// The selector is PageAgentContext.SurfaceRootSelector, the shell's main column.
// A missing document or a disconnected circuit returns null; page_context then still
// carries title, purpose, and route from the page registry.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Components;

[SideEffectService]
internal static class PageSurfaceJsModule
{
  internal const string Specifier = "./js/features/page-surface.js";

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
