#region Purpose
// Selects the WebAuthn Relying Party for a ceremony from the request host, matched against
// WebAuthnOptions.AllowedRpIds — the per-request RP-ID selection at the heart of task 104-031.
#endregion

#region Design
// Pure and static (no injected state): the five identity handlers call Select as the FIRST step of
// Handle — before issuing or consuming any challenge — so a disallowed host never burns a challenge.
// The request host arrives via IRequestHostAccessor (web-server implementation); this method takes
// the already-read host string so it stays host-free and unit-testable without ASP.NET Core.
//
// Match is case-INSENSITIVE (OrdinalIgnoreCase) because DNS host names are case-insensitive, but the
// returned WebAuthnRelyingParty.Id is the ALLOWLIST entry's CANONICAL casing, not the request's — the
// RP ID that flows into ceremony options and rpIdHash verification must be the operator-approved
// spelling, never attacker-influenced casing echoed back from the Host header.
//
// FAIL-CLOSED, no host echo, no fallback: null/empty/unlisted host returns a fixed 400 problem whose
// Detail deliberately does NOT contain the requested host (no reflection of attacker-controlled input
// into the response body) and never substitutes a default RP ID. The allowlist is the entire trust
// boundary — a forged Host can only ever select among already-approved RP IDs.
//
// RpName and AllowedOrigins flow straight through from options onto the constructed relying party
// (AllowedOrigins is a flat list shared across all RP IDs — see WebAuthnOptions's Design region for
// that caveat), so the empty-AllowedOrigins "accept any https origin whose host equals the selected
// RP ID" rule keys off THIS selection's Id.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public static class WebAuthnRelyingPartySelection
{
  public static OneOf<WebAuthnRelyingParty, SharedProblemDetails> Select(string? requestHost, WebAuthnOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    if (!string.IsNullOrEmpty(requestHost))
    {
      foreach (string allowedRpId in options.AllowedRpIds)
      {
        if (string.Equals(allowedRpId, requestHost, StringComparison.OrdinalIgnoreCase))
        {
          // Return the allowlist entry's canonical casing, never the request's — see Design region.
          return new WebAuthnRelyingParty(allowedRpId, options.RpName, options.AllowedOrigins);
        }
      }
    }

    return new SharedProblemDetails
    {
      Title = "Host not allowed",
      Status = 400,
      Detail = "Passkeys are not enabled for this host."
    };
  }
}
