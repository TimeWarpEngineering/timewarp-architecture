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
// Counter JS interop (Spa.Counter.*) still uses the initializer; this helper is passkey-only.
// Dispose the module reference after each call — the ES module stays cached in the browser.
#endregion

namespace TimeWarp.Architecture.Services;

internal static class WebAuthnJsModule
{
  internal const string Specifier = "./js/features/web-authn.js";

  internal static Task<string> CreateCredentialAsync(
    IJSRuntime jsRuntime,
    string optionsJson,
    bool preferHybrid,
    CancellationToken cancellationToken) =>
    InvokeAsync(jsRuntime, "CreateCredential", optionsJson, preferHybrid, cancellationToken);

  internal static Task<string> GetCredentialAsync(
    IJSRuntime jsRuntime,
    string optionsJson,
    bool preferHybrid,
    CancellationToken cancellationToken) =>
    InvokeAsync(jsRuntime, "GetCredential", optionsJson, preferHybrid, cancellationToken);

  private static async Task<string> InvokeAsync(
    IJSRuntime jsRuntime,
    string exportName,
    string optionsJson,
    bool preferHybrid,
    CancellationToken cancellationToken)
  {
    IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>(
      "import",
      cancellationToken,
      Specifier);
    try
    {
      return await module.InvokeAsync<string>(
        exportName,
        cancellationToken,
        optionsJson,
        preferHybrid);
    }
    finally
    {
      await module.DisposeAsync();
    }
  }
}
