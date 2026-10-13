#region Purpose
// On-demand IJSRuntime import of web-authn.js named exports so passkey ceremonies do not require window.Spa.
#endregion

#region Design
// Task 200: Login and Settings used Spa.WebAuthn.* string identifiers. Blazor resolves those on
// window.Spa, which exists only when the host JS initializer list includes Web.Spa. A stale
// web-server.modules.json omitted that initializer and /Login threw "'Spa' was undefined" on both
// passkey buttons. import("./js/features/web-authn.js") loads the same named exports (CreateCredential /
// GetCredential) without the global. Specifier "./js/features/web-authn.js" resolves via
// <base href="/" /> to /js/features/web-authn.js on every route; MapStaticAssets serves
// the unfingerprinted path (and a fingerprinted twin). No <ImportMap /> in App.razor.
// The Counter demo's JS button (onclick → Spa.Counter.*) still uses the initializer; this helper
// is passkey-only.
// Task 350: SendJsonAsync is the browser fetch leg BrowserFetchHttpMessageHandler uses for
// session-issuing ceremony completions, so the Set-Cookie lands in the browser in every render mode.
// Dispose the module reference after each call — the ES module stays cached in the browser.
// [SideEffectService] (task 265): components never call this directly — TWA0026; action handlers do.
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class WebAuthnJsModule
{
  internal const string Specifier = "./js/features/web-authn.js";

  internal static Task<string> CreateCredentialAsync(
    IJSRuntime jsRuntime,
    string optionsJson,
    bool preferHybrid,
    CancellationToken cancellationToken) =>
    InvokeAsync(jsRuntime, "CreateCredential", cancellationToken, optionsJson, preferHybrid);

  internal static Task<string> GetCredentialAsync(
    IJSRuntime jsRuntime,
    string optionsJson,
    bool preferHybrid,
    CancellationToken cancellationToken) =>
    InvokeAsync(jsRuntime, "GetCredential", cancellationToken, optionsJson, preferHybrid);

  /// <summary>Browser fetch with credentials; returns the JSON { status, contentType, body } envelope.</summary>
  internal static Task<string> SendJsonAsync(
    IJSRuntime jsRuntime,
    string method,
    string url,
    string? bodyJson,
    CancellationToken cancellationToken) =>
    InvokeAsync(jsRuntime, "SendJson", cancellationToken, method, url, bodyJson);

  private static async Task<string> InvokeAsync(
    IJSRuntime jsRuntime,
    string exportName,
    CancellationToken cancellationToken,
    params object?[] arguments)
  {
    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>(
        "import",
        cancellationToken,
        Specifier);
      return await module.InvokeAsync<string>(
        exportName,
        cancellationToken,
        arguments);
    }
    catch (JSDisconnectedException exception)
    {
      throw new InvalidOperationException("The browser circuit disconnected during the passkey ceremony.", exception);
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
