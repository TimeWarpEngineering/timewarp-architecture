#region Purpose
// Paths and antiforgery-token shape for the browser-navigated sign-out (SPA JS helper + FastEndpoints).
#endregion

#region Design
// Task 251: sign-out must be a BROWSER request in every render mode. Under InteractiveServer the
// SPA handler runs in the circuit, so an IWebServerApiService POST is a server→server self-call and
// the cookie-deletion Set-Cookie lands on that HttpClient response, never in the browser.
// Not [ApiRoute]/[ApiEndpoint] (same reason as ChallengeEntra): the sign-out response is a 303
// redirect to /Login, not mediator JSON, and the token read is a browser-only precondition.
// Two paths, one flow (sign-out.ts):
//   GET  AntiforgeryTokenPath → { formFieldName, requestToken } + antiforgery cookie on the BROWSER
//   POST Path (form, token field) → EndBrowserSession handler → expired cookie + 303 /Login
// The token is fetched by the browser just-in-time rather than read from AntiforgeryStateProvider
// because that provider only carries a token when the page was prerendered (Prerender is a
// setting); a browser fetch works for WebAssembly, Server and Auto, prerendered or not.
// Paths live outside /api: YARP routes non-/api paths to web-server via its catch-all, and the
// identity-session challenge classifier treats them as page paths (irrelevant — both anonymous).
// With no [Operation] to generate them, the OpenAPI operation ids and summaries are the
// constants here; each endpoint passes its pair as OpenApiOperationName metadata (task 458-005).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static class SignOutBrowserSession
{
  /// <summary>The OpenAPI operation id of the sign-out form post. The endpoint is hand-written, so no [Operation] generates it.</summary>
  public const string OperationName = "browser-session.sign-out";

  /// <summary>The OpenAPI summary of <see cref="OperationName"/>.</summary>
  public const string OperationDescription = "End the browser's identity session from a sign-out form post.";

  /// <summary>The OpenAPI operation id of the antiforgery-token read that precedes the sign-out post.</summary>
  public const string AntiforgeryTokenOperationName = "browser-session.get-sign-out-antiforgery-token";

  /// <summary>The OpenAPI summary of <see cref="AntiforgeryTokenOperationName"/>.</summary>
  public const string AntiforgeryTokenOperationDescription = "Get the antiforgery form field and token for the sign-out form post.";

  public const string Path = "/identity/sign-out";
  public const string AntiforgeryTokenPath = "/identity/sign-out/antiforgery-token";
  public const string RedirectPath = "/Login";

  public sealed record AntiforgeryToken(string FormFieldName, string RequestToken);
}
