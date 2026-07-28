#region Purpose
// Cookie-backed IBrowserSessionService implementation: SignInAsync/AuthenticateAsync against the
// named identity-session scheme (Program.ConfigureAuthentication registers it via AddCookie).
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), not singleton — HttpContext is per-request state.
// IssueAsync writes only PrincipalIdClaimType (+ optional display name under ClaimTypes.Name);
// GetCurrentPrincipalIdAsync reads it back and parses to Guid then PrincipalId.From — an empty or
// unparsable claim value returns null rather than throwing, since a corrupted/foreign cookie should
// read as "not authenticated," not crash the request.
// cancellationToken parameters are unused: ASP.NET Core's HttpContext.SignInAsync/AuthenticateAsync
// extension methods take no CancellationToken (a known Authentication API gap) — the port's own
// signature still carries one (IBrowserSessionService is not ASP.NET-specific), so this
// implementation simply cannot honor it.
#endregion

namespace TimeWarp.Architecture.Services;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;

public sealed class CookieBrowserSessionService : IBrowserSessionService
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public CookieBrowserSessionService(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public async Task IssueAsync(PrincipalId principalId, string? displayName, CancellationToken cancellationToken)
  {
    HttpContext httpContext = HttpContextAccessor.HttpContext
      ?? throw new InvalidOperationException("IssueAsync requires an active HttpContext.");

    List<Claim> claims = [new Claim(IdentitySessionDefaults.PrincipalIdClaimType, principalId.Value.ToString())];

    if (!string.IsNullOrWhiteSpace(displayName))
    {
      claims.Add(new Claim(ClaimTypes.Name, displayName));
    }

    var identity = new ClaimsIdentity(claims, IdentitySessionDefaults.Scheme);
    var claimsPrincipal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(IdentitySessionDefaults.Scheme, claimsPrincipal);
  }

  public async Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken)
  {
    HttpContext? httpContext = HttpContextAccessor.HttpContext;
    if (httpContext is null) return null;

    AuthenticateResult result = await httpContext.AuthenticateAsync(IdentitySessionDefaults.Scheme);
    if (!result.Succeeded || result.Principal is null) return null;

    string? claimValue = result.Principal.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty) return null;

    return PrincipalId.From(guid);
  }
}
