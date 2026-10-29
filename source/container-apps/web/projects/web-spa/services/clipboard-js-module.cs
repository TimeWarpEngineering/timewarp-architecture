#region Purpose
// On-demand IJSRuntime import of clipboard.js: write plain text to the system clipboard.
#endregion

#region Design
// Same import-a-named-export shape as SignOutJsModule. A clipboard rejection (permission
// denied, insecure context) surfaces as JSException so the caller can report it. A dropped
// Server circuit (JSDisconnectedException) is swallowed: there is no page left to report to.
// TryWriteTextAsync is the one place the copy handlers classify a failure; WriteTextAsync is
// private, so a handler cannot bypass it. A JSException, or an OperationCanceledException while
// the caller's token is live (a JS-interop timeout, e.g. a stalled permission prompt on
// Server), is Denied; a cancelled caller token means the host was disposed, so it is Cancelled
// and the caller does nothing. DeniedBody is the one Warning body every copy handler shows on
// Denied; each handler keeps its own title and state update. [SideEffectService]: components
// never call this directly (TWA0026); the copy handlers do. wwwroot/js is emitted by
// TypeScript.MSBuild and is not committed.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class ClipboardJsModule
{
  internal const string Specifier = "./js/features/clipboard.js";
  internal const string WriteTextExport = "WriteText";
  internal const string DeniedBody = "The browser blocked clipboard access. Select the text and copy it instead.";

  internal enum WriteResult
  {
    Written,
    Denied,
    Cancelled,
  }

  /// <summary>Writes text and reports Written, Denied (rejection or interop timeout), or Cancelled (caller token).</summary>
  internal static async Task<WriteResult> TryWriteTextAsync(IJSRuntime jsRuntime, string text, CancellationToken cancellationToken)
  {
    try
    {
      await WriteTextAsync(jsRuntime, text, cancellationToken);
      return WriteResult.Written;
    }
    catch (Exception exception) when
      (exception is JSException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
    {
      return WriteResult.Denied;
    }
    catch (OperationCanceledException)
    {
      return WriteResult.Cancelled;
    }
  }

  private static async Task WriteTextAsync(IJSRuntime jsRuntime, string text, CancellationToken cancellationToken)
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
