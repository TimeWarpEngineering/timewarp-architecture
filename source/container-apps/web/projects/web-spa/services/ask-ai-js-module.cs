#region Purpose
// On-demand import of ask-ai.js: insert an @ token, check an @ starts a word, refocus the
// composer, keep and restore the panel's opener, copy answer text, set the assistant avatar the
// turns show, and capture or insert a dictated prompt.
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

  /// <summary>On open: keeps the focused element (or the palette's return target) to restore on close.</summary>
  internal static Task CaptureOpenerAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "CaptureOpener");
  }

  /// <summary>On open: focuses the composer, waiting a few frames for the library to attach it.</summary>
  internal static Task FocusComposerOnOpenAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "FocusComposerOnOpen");
  }

  /// <summary>Focuses the composer only when focus is in the panel or on nothing.</summary>
  internal static Task FocusComposerInPanelAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "FocusComposerInPanel");
  }

  /// <summary>On close: focuses the opener, or the page main region when it is gone.</summary>
  internal static Task RestoreOpenerAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeAsync(jsRuntime, "RestoreOpener");
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

  /// <summary>Starts the microphone. Error is denied, unavailable, or failed. No audio yet.</summary>
  internal static Task<AskVoiceCaptureResult> StartVoiceCaptureAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeCaptureAsync(jsRuntime, "StartVoiceCapture");
  }

  /// <summary>Stops the microphone and returns base64 audio, or an empty error.</summary>
  internal static Task<AskVoiceCaptureResult> StopVoiceCaptureAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    return InvokeCaptureAsync(jsRuntime, "StopVoiceCapture");
  }

  /// <summary>Drops a live capture. Safe when nothing is recording.</summary>
  internal static async Task CancelVoiceCaptureAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    try
    {
      await using IJSObjectReference? module = await ImportAsync(jsRuntime);
      if (module is null)
      {
        return;
      }

      await module.InvokeVoidAsync("CancelVoiceCapture");
    }
    catch (JSException)
    {
    }
    catch (JSDisconnectedException)
    {
    }
    catch (InvalidOperationException)
    {
    }
  }

  /// <summary>Inserts text at the composer caret and focuses it. False when the composer is gone.</summary>
  internal static async Task<bool> InsertComposerTextAsync(IJSRuntime jsRuntime, string text)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentNullException.ThrowIfNull(text);
    try
    {
      await using IJSObjectReference? module = await ImportAsync(jsRuntime);
      if (module is null)
      {
        return false;
      }

      return await module.InvokeAsync<bool>("InsertComposerText", text);
    }
    catch (JSException)
    {
      return false;
    }
    catch (JSDisconnectedException)
    {
      return false;
    }
    catch (InvalidOperationException)
    {
      return false;
    }
  }

  private static async Task<AskVoiceCaptureResult> InvokeCaptureAsync(IJSRuntime jsRuntime, string export)
  {
    try
    {
      await using IJSObjectReference? module = await ImportAsync(jsRuntime);
      if (module is null)
      {
        return Unavailable();
      }

      AskVoiceCaptureResult? result = await module.InvokeAsync<AskVoiceCaptureResult>(export);
      return result ?? Unavailable();
    }
    catch (JSException)
    {
      return new AskVoiceCaptureResult { Error = "failed" };
    }
    catch (JSDisconnectedException)
    {
      return Unavailable();
    }
    catch (InvalidOperationException)
    {
      return Unavailable();
    }
  }

  private static async Task<IJSObjectReference?> ImportAsync(IJSRuntime jsRuntime)
  {
    try
    {
      return await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        CancellationToken.None,
        Specifier
      );
    }
    catch (JSDisconnectedException)
    {
      return null;
    }
    catch (InvalidOperationException)
    {
      return null;
    }
    catch (JSException)
    {
      return null;
    }
  }

  private static AskVoiceCaptureResult Unavailable() => new() { Error = "unavailable" };

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
