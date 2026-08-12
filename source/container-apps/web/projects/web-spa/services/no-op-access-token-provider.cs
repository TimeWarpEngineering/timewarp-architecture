#region Purpose
// IAccessTokenProvider that never yields a bearer token — identity-session uses cookies, not MSAL.
#endregion

#region Design
// Registered for the default non-mock / non-Entra SPA path (task 104-021). BaseApiService still
// depends on IAccessTokenProvider; a missing token degrades to anonymous Authorization header so
// the server's cookie + 401 path remains the source of truth. Do not register outside
// IdentitySessionAuthenticationRegistration (mirrors mock registration discipline).
// AccessTokenResult ctor is positional (status, token, redirectUrl, options) — package versions
// differ on nullable annotations; use empty token + empty redirectUrl for RequiresRedirect.
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Satisfies DI for <see cref="IAccessTokenProvider"/> without acquiring Entra/MSAL tokens.
/// </summary>
public sealed class NoOpAccessTokenProvider : IAccessTokenProvider
{
  private static readonly AccessToken EmptyToken = new()
  {
    Value = string.Empty,
    Expires = DateTimeOffset.MinValue,
  };

  public ValueTask<AccessTokenResult> RequestAccessToken() =>
    new(new AccessTokenResult(
      AccessTokenResultStatus.RequiresRedirect,
      EmptyToken,
      string.Empty,
      null));

  public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options) =>
    RequestAccessToken();
}
