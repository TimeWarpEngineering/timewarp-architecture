#region Purpose
// Sets WASM fetch credentials so same-origin API calls send the identity-session cookie.
#endregion

#region Design
// Blazor WASM HttpClient uses browser fetch. Without SameOrigin/Include, fetch omits cookies,
// so identity-session [EndpointAuthorize] APIs 401 even when the document request carried a
// valid cookie (InteractiveAuto after WASM switch, or InteractiveWebAssembly). SameOrigin
// matches SPA HttpClient.BaseAddress = HostEnvironment.BaseAddress. The option is ignored by
// non-browser handlers (server SocketsHttpHandler).
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Http;

/// <summary>
/// Marks outgoing WASM requests to include same-origin cookies.
/// </summary>
public sealed class BrowserRequestCredentialsHandler : DelegatingHandler
{
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    request.SetBrowserRequestCredentials(BrowserRequestCredentials.SameOrigin);
    return base.SendAsync(request, cancellationToken);
  }
}
