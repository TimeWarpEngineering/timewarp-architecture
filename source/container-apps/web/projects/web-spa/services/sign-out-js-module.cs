#region Purpose
// On-demand IJSRuntime import of sign-out.js so sign-out is a browser form POST in every render mode.
#endregion

#region Design
// Task 251: same import-a-named-export shape as WebAuthnJsModule (no window.Spa dependency).
// The export fetches the antiforgery token and submits the sign-out form in the browser; the
// resulting full navigation unloads the page (WebAssembly) or the circuit (Server), so callers
// must not NavigateTo afterwards. Module dispose tolerates JSDisconnectedException: under Server
// the unload can drop the circuit before the dispose round-trip completes. Paths are
// SignOutBrowserSession constants — the SSOT shared with the server endpoints.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Features.Identity;

internal static class SignOutJsModule
{
  internal const string Specifier = "./js/features/sign-out.js";
  internal const string ExportName = "SignOut";

  internal static async Task SignOutAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
  {
    IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>(
      "import",
      cancellationToken,
      Specifier);
    try
    {
      await module.InvokeVoidAsync(
        ExportName,
        cancellationToken,
        SignOutBrowserSession.AntiforgeryTokenPath,
        SignOutBrowserSession.Path);
    }
    finally
    {
      try
      {
        await module.DisposeAsync();
      }
      catch (JSDisconnectedException)
      {
        // The form submit already unloaded the page / dropped the Server circuit.
      }
    }
  }
}
