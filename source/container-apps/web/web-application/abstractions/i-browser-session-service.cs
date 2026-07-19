#region Purpose
// Port for issuing/reading the browser's authenticated session so identity handlers never depend on
// ASP.NET Core's HttpContext/SignInManager directly.
#endregion

#region Design
// complete-passkey-registration-handler and complete-passkey-authentication-handler call IssueAsync
// after a successful WebAuthn ceremony; get-current-session-handler calls
// GetCurrentPrincipalIdAsync. The port is deliberately thin (two methods, no general claims/identity
// surface) — today's only implementation is a cookie (web-server's CookieBrowserSessionService), but
// nothing here names "cookie."
#endregion

namespace TimeWarp.Architecture.Abstractions;

public interface IBrowserSessionService
{
  Task IssueAsync(PrincipalId principalId, string? displayName, CancellationToken cancellationToken);

  Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken);
}
