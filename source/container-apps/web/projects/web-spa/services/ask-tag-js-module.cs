#region Purpose
// On-demand import of ask-tag.js: run Ask Tag pick mode and cancel it.
#endregion

#region Design
// [SideEffectService] (TWA0026): only AskTagState's StartPick, CancelPick and SessionChanged
// handlers call this. A pick is named by StartPick's token (task 351). PickAsync arms pick mode
// (Start), then asks the handler whether the pick is still current (armed): only then does the
// panel say "picking", because every listener is already live. A pick that went stale while it
// armed is cancelled here, so no armed pick outlives its handler. It then waits for the answer
// (Answer) with the handler's token and never the runtime's default call timeout (Blazor Server's
// one-minute interop timeout would cancel a slow pick). The module answers JSON, or "" when the
// pick was cancelled; a refused Start, a fake runtime's empty answer, bad JSON, a JS failure or a
// dropped circuit all read as cancelled (null), so the handler only clears the flag.
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

  /// <summary>
  /// Runs pick <paramref name="token"/> until the person chooses (the candidate) or cancels (null).
  /// <paramref name="armed"/> runs once the browser catches clicks and keys; false cancels the pick.
  /// </summary>
  internal static async Task<AskTagCandidate?> PickAsync
  (
    IJSRuntime jsRuntime,
    string token,
    Func<bool> armed,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentException.ThrowIfNullOrEmpty(token);
    ArgumentNullException.ThrowIfNull(armed);
    string? json;
    try
    {
      await using IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        cancellationToken,
        Specifier
      );
      if (!await module.InvokeAsync<bool>("Start", cancellationToken, PageAgentContext.SurfaceRootSelector, token))
      {
        return null;
      }

      if (!armed())
      {
        await module.InvokeVoidAsync("CancelPick", token);
        return null;
      }

      json = await module.InvokeAsync<string?>("Answer", cancellationToken, token);
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

  /// <summary>Ends pick <paramref name="token"/>, or stops it from arming when its Start is still on the way.</summary>
  internal static async Task CancelPickAsync(IJSRuntime jsRuntime, string token)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentException.ThrowIfNullOrEmpty(token);
    try
    {
      await using IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>
      (
        "import",
        CancellationToken.None,
        Specifier
      );
      await module.InvokeVoidAsync("CancelPick", token);
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException)
    {
      // Nothing is picking, or the circuit is already gone.
    }
  }
}
