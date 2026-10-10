#region Purpose
// On-demand import of ask-ai.js: insert an @ token and copy answer text.
#endregion

#region Design
// Same import-a-named-export shape as CommandPaletteJsModule. The textarea belongs to the library
// chat control, so the insertion stays in the browser. Copy takes the text from C# (the last
// answer the thumbs also use) and only writes it to the clipboard.
// Callers opt out of TWA0026 with DirectComponentSideEffect. wwwroot/js is emitted by
// TypeScript.MSBuild and is not committed.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class AskAiJsModule
{
  internal const string Specifier = "./js/features/ask-ai.js";

  internal static async Task InsertReferenceAsync(IJSRuntime jsRuntime, string token)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentNullException.ThrowIfNull(token);
    await InvokeAsync(jsRuntime, "InsertReference", token);
  }

  internal static async Task CopyTextAsync(IJSRuntime jsRuntime, string text)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentNullException.ThrowIfNull(text);
    await InvokeAsync(jsRuntime, "CopyText", text);
  }

  private static async Task InvokeAsync(IJSRuntime jsRuntime, string export, params object?[] arguments)
  {
    try
    {
      await using IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        CancellationToken.None,
        Specifier
      );
      await module.InvokeVoidAsync(export, arguments);
    }
    catch (JSDisconnectedException)
    {
      // The circuit is already gone.
    }
  }
}
