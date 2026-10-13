#region Purpose
// On-demand import of ask-tag.js: run Ask Tag pick mode and cancel it.
#endregion

#region Design
// [SideEffectService] (TWA0026): only AskTagState's StartPick and CancelPick handlers call this.
// PickAsync waits for the person, so it passes the handler's token and never the runtime's default
// call timeout (Blazor Server's one-minute interop timeout would cancel a slow pick). The module
// answers JSON, or "" when the pick was cancelled; a fake runtime's empty answer, bad JSON, a JS
// failure or a dropped circuit all read as cancelled (null), so the handler only clears the flag.
// The pick root is PageAgentContext.SurfaceRootSelector, the same page body page_context walks.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.Applications;

[SideEffectService]
internal static class AskTagJsModule
{
  internal const string Specifier = "./js/features/ask-tag.js";

  private static readonly JsonSerializerOptions CandidateJson = new(JsonSerializerDefaults.Web);

  /// <summary>Runs pick mode until the person chooses (the candidate) or cancels (null).</summary>
  internal static async Task<AskTagCandidate?> PickAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    string? json;
    try
    {
      await using IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        cancellationToken,
        Specifier
      );
      json = await module.InvokeAsync<string?>("Pick", cancellationToken, PageAgentContext.SurfaceRootSelector);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException)
    {
      return null;
    }

    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<AskTagCandidate>(json, CandidateJson);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  internal static async Task CancelPickAsync(IJSRuntime jsRuntime)
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
      await module.InvokeVoidAsync("CancelPick");
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException)
    {
      // Nothing is picking, or the circuit is already gone.
    }
  }
}
