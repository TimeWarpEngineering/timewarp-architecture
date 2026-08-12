#region Purpose
// Port for issuing/reading/ending the browser's authenticated session so identity handlers never
// depend on ASP.NET Core's HttpContext/SignInManager directly.
#endregion

#region Design
// complete-passkey-registration-handler and complete-passkey-authentication-handler call IssueAsync
// after a successful WebAuthn ceremony; get-current-session-handler calls
// GetCurrentPrincipalIdAsync; end-browser-session-handler (task 104-034) calls SignOutAsync.
// The port is deliberately thin (no general claims/identity surface) — today's only implementation
// is a cookie (web-server's CookieBrowserSessionService), but nothing here names "cookie."
// SignOutAsync is idempotent: no ambient session is success, not an error (SPA sign-out must not
// 401 when already anonymous).
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IBrowserSessionService
{
  Task IssueAsync(PrincipalId principalId, string? displayName, CancellationToken cancellationToken);

  Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken);

  /// <summary>Clears the ambient browser session if present. No-op when already signed out.</summary>
  Task SignOutAsync(CancellationToken cancellationToken);
}
