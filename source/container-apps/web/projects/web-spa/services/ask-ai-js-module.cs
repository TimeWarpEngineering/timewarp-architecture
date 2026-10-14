#region Purpose
// On-demand import of ask-ai.js: insert an @ token, check an @ starts a word, refocus the
// composer, copy answer text, and set the assistant avatar the turns show.
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

  /// <summary>On the @ keydown: true when the caret starts a word (nothing or whitespace before it).</summary>
  internal static async Task<bool> AtStartsWordAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    try
    {
      await using IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        CancellationToken.None,
        Specifier
      );
      return await module.InvokeAsync<bool>("AtStartsWord");
    }
    catch (JSDisconnectedException)
    {
      return false;
    }
  }

  internal static Task FocusComposerAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "FocusComposer");
  }

  internal static async Task CopyTextAsync(IJSRuntime jsRuntime, string text)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentNullException.ThrowIfNull(text);
    await InvokeAsync(jsRuntime, "CopyText", text);
  }

  /// <summary>Sets the avatar drawn beside assistant turns; null or a non-data-URI clears it.</summary>
  internal static Task SetAssistantAvatarAsync(IJSRuntime jsRuntime, string? dataUri)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "SetAssistantAvatar", dataUri);
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
