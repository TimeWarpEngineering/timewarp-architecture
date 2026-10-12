#region Purpose
// On-demand IJSRuntime import of clipboard.js: write plain text to the system clipboard.
#endregion

#region Design
// Same import-a-named-export shape as SignOutJsModule. A clipboard rejection (permission denied,
// insecure context) surfaces as JSException so the caller can report it. A dropped Server
// circuit (JSDisconnectedException) is swallowed: there is no page left to report to.
// [SideEffectService]: components never call this directly (TWA0026); the CopyMessage handler
// does. wwwroot/js is emitted by TypeScript.MSBuild and is not committed.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class ClipboardJsModule
{
  internal const string Specifier = "./js/features/clipboard.js";
  internal const string WriteTextExport = "WriteText";

  internal static async Task WriteTextAsync(IJSRuntime jsRuntime, string text, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentNullException.ThrowIfNull(text);
    try
    {
      IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        cancellationToken,
        Specifier
      );
      try
      {
        await module.InvokeVoidAsync(WriteTextExport, cancellationToken, text);
      }
      finally
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
    catch (JSDisconnectedException)
    {
      // Server: the circuit dropped; there is no page left to write from.
    }
  }
}
