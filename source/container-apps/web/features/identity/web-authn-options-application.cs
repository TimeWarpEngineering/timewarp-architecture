#region Purpose
// Relying Party configuration for the WebAuthn passkey ceremonies (task 104-003; per-request RP-ID
// selection added in 104-031).
#endregion

#region Design
// Compiles into web-application (namespace TimeWarp.Architecture.Features.Identity.Application,
// filename suffix -application.cs), not web-server, despite being bound from configuration at the
// host (web-server's Program.ConfigureSettings calls AddFluentValidatedOptions<WebAuthnOptions,
// WebAuthnOptionsValidator> there) — the identity handlers that consume IOptions<WebAuthnOptions>
// compile into web-application, which does NOT reference web-server (web-server -> web-infrastructure
// -> web-application is the one-way dependency chain), so the type has to live where both the
// binder (web-server, which references web-application transitively) and the consumers
// (the identity handlers, same namespace, no using needed) can see it. SampleOptions stays under
// web-server/configuration/ (Configuration namespace) precisely because nothing outside web-server
// ever consumes it — this is the one place that convention does not fit.
//
// PER-REQUEST RP-ID SELECTION (task 104-031): there is no single static RpId. AllowedRpIds is an
// allowlist of RP IDs this application may serve passkeys under; the effective RP ID for a given
// ceremony is chosen PER REQUEST from the request's Host header, matched (case-insensitively)
// against this list — see WebAuthnRelyingPartySelection.Select. A single running server can thus
// serve both its localhost dev origin and a public share hostname (e.g. arch.timewarp.work) without
// restart. The old single-value RpId property was REMOVED outright (not deprecated): keeping it
// would let a stale RpId secret bind to a property nothing reads.
//
// FAIL-CLOSED: a request whose Host is not in AllowedRpIds gets a 400 problem-details, never a
// fallback to some default RP ID (the browser would reject a mismatched RP ID opaquely, and
// deriving the RP ID from an arbitrary attacker-controlled Host would let a forged Host mint
// credentials for an arbitrary RP ID). The allowlist is the whole security boundary here: a forged
// Host can only ever SELECT among already-approved RP IDs, never expand them.
// MEMBERSHIP ORACLE (round-1 security S2, accepted): the precise fail-closed claim is that the
// selected/requested host is never REFLECTED in the response body (the 400 Detail is a fixed string),
// NOT that there is no enumeration whatsoever — the 200-vs-400 status on the anonymous Start endpoints
// IS an observable allowlist-MEMBERSHIP signal (probe a hostname, learn whether it is enabled). This
// is low value (the set of hostnames an app serves is inherently public — it is in DNS and TLS SNI)
// and deliberately not hardened away; do not restate this as "no enumeration."
//
// DEFAULT ["localhost"]: the shared host component of both the dev origin (https://localhost:63611)
// and the fixed integration-test origin (https://localhost:7000), so the template works out of the
// box, zero-config.
//
// BINDER APPEND SEMANTICS: AllowedRpIds is initialized to ["localhost"] as a C# default. The
// Microsoft.Extensions.Configuration binder APPENDS bound entries onto a pre-initialized List<T>
// rather than replacing it, so an appsettings/user-secret entry like AllowedRpIds:0=arch.timewarp.work
// yields the EFFECTIVE list ["localhost","arch.timewarp.work"] — the developer adds a personal share
// hostname via user secrets (Ingress:PublicUrl precedent) without losing localhost, and without
// touching committed config. CONSEQUENCE: shipped appsettings.json MUST NOT list AllowedRpIds — a
// committed "localhost" entry would append to the default and produce ["localhost","localhost"]. The
// binding regression test (WebAuthnOptions_Binding_Tests) pins this append behavior so a framework
// change cannot silently flip it to replace.
// APPEND-ONLY CONSEQUENCE (round-1 security S1, document-and-accept): because config can only ADD to
// the ["localhost"] default and never REMOVE from it, "localhost" is permanently in AllowedRpIds even
// in a hardened production deployment — there is no config to take it out. Combined with the empty-
// AllowedOrigins dev fallback, that means production will accept a passkey ceremony whose selected
// rp.id is "localhost" (from a request that arrives with Host: localhost). This is confined to
// LOOPBACK self-registration and is NOT remotely exploitable: a remote attacker cannot make the real
// ingress chain deliver Host: localhost to the server AND cannot satisfy the browser-enforced origin
// binding + the server rpIdHash check for rp.id=localhost from a non-loopback origin — both would have
// to be "localhost" too. Accepted as the price of the zero-config template default; an operator who
// wants localhost gone entirely must set AllowedOrigins explicitly (which then no longer accepts the
// bare host==rp.id fallback) rather than expecting to un-list localhost.
//
// RP-ID CREDENTIAL SCOPING (WebAuthn design, not a bug): a passkey is bound to the exact RP ID it
// was registered under. A passkey registered under "arch.timewarp.work" will NOT surface for
// "localhost" and vice versa — the browser scopes discoverable credentials by RP ID. This is
// inherent to WebAuthn, so a credential registered on one host simply not appearing on another must
// not be filed as a bug (see also the PasskeysPage note).
//
// AllowedOrigins defaults to empty, which WebAuthnRelyingParty treats as "accept any https origin
// whose host equals the SELECTED RP ID" (see its Design region) — this deliberately covers both
// fixed local origins above without hand-listing ports; production configuration should set
// AllowedOrigins explicitly. CAVEAT (interplay with per-request selection): AllowedOrigins is a
// FLAT list shared across ALL entries in AllowedRpIds, not partitioned per RP ID. With the empty-list
// dev fallback this is fine (each ceremony keys the host==selected-RP-ID rule off whichever RP ID was
// selected). But if you populate AllowedOrigins explicitly AND serve multiple RP IDs, every listed
// origin is accepted for every selected RP ID — an origin allowed for RP ID A is also accepted when
// RP ID B is the selected party (round-1 security S3: a multi-RP-ID production footgun). This is
// currently only theoretical, blocked one layer down by two independent checks that do partition by
// RP ID: the BROWSER's rule that an origin's host must be an rpId-suffix match of the ceremony's
// rp.id, and the SERVER's rpIdHash comparison in WebAuthnRegistration/WebAuthnAuthentication.Verify —
// a credential minted for rp.id=B cannot be presented from A's origin regardless of what AllowedOrigins
// lists. Per-RP-ID origin partitioning is deliberately out of scope here; if a future config genuinely
// needs distinct origin sets per RP ID, partition AllowedOrigins by RP ID rather than relying on those
// lower layers.
//
// Configuration section name is "WebAuthnOptions" (matches this type's name), not "WebAuthn":
// AddFluentValidatedOptions binds `configuration.GetSection(key)` where key defaults to
// typeof(TOptions).Name absent a [ConfigurationKey] attribute (see TimeWarp.OptionsValidation).
// A round-1 review caught appsettings.json shipping a "WebAuthn" section that silently never bound
// (masked because the shipped values equalled these C# defaults) — see
// WebServerIntegrationTests's WebAuthnOptions binding test for the regression pin.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public class WebAuthnOptions
{
  public List<string> AllowedRpIds { get; set; } = ["localhost"];
  public string RpName { get; set; } = "TimeWarp Architecture";
  public List<string> AllowedOrigins { get; set; } = [];
}
