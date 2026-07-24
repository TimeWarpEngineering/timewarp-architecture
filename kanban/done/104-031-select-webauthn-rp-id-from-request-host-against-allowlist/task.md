# Select WebAuthn RP ID from request host against allowlist

## Description

Discovered during task 112's off-LAN verification (2026-07-21): the passkey ceremony fails on
`https://arch.timewarp.work` with "The relying party ID is not a registrable domain suffix of,
nor equal to the current domain" — `WebAuthnOptions.RpId` is a single static value (default
`localhost`), so the same running server cannot serve passkeys to both its localhost dev origin
and a public share hostname (the *.timewarp.work chain from task 112). Interim workaround:
`WebAuthnOptions__RpId=arch.timewarp.work dev run` (env override; flips the breakage to
localhost while set).

Fix: derive the effective RP ID per request from the request's host, validated against an
allowlist, instead of one static value.

## Requirements

- `WebAuthnOptions` gains an `AllowedRpIds` list (default `["localhost"]` — template still works
  out of the box, zero-config). A request whose host matches an entry uses that host as RP ID;
  non-matching hosts fail closed (400/problem-details, not a fallback to a wrong RP ID that the
  browser will reject opaquely).
- Both ceremonies (registration + authentication) and origin validation
  (`WebAuthnRelyingParty`) use the same per-request selection; the "empty AllowedOrigins accepts
  any https origin whose host equals RpId" rule keys off the *selected* RP ID.
- Credentials are RP-ID-scoped by WebAuthn design: a passkey registered under
  `arch.timewarp.work` will not surface for `localhost` and vice versa — document this in the
  Passkeys demo page or options Design region so it doesn't get filed as a bug.
- Forwarded-headers correctness: behind the task-112 Caddy proxy the server must see the public
  host (X-Forwarded-Host / Host pass-through) — verify what the Aspire YARP ingress forwards and
  that `UseForwardedHeaders` (or equivalent) is wired so `HttpContext.Request.Host` is the
  public name, not localhost:63621.
- Update `WebAuthnOptionsValidator` (allowlist entries must be valid DNS names, no scheme/port),
  plus the options-binding regression test.

## Checklist

- [ ] `AllowedRpIds` option + validator + binding test
- [ ] Per-request RP ID selection in start/complete handlers for both ceremonies, fail-closed
- [ ] `WebAuthnRelyingParty` origin check uses selected RP ID
- [ ] Forwarded-host correctness through YARP ingress + Caddy (integration test with
      X-Forwarded-Host)
- [ ] Unit + integration tests: allowlisted host succeeds, non-allowlisted host 400s,
      localhost default unchanged
- [ ] Document RP-ID scoping of credentials (passkeys don't roam between hostnames)
- [ ] Remove the env-override workaround note from task 112 runbook once landed

## Notes

- Origin story: task 112 (`kanban/*/112-.../task.md`, public-path runbook) — the shared
  `*.timewarp.work` ingress makes "same app, multiple hostnames" the normal case, and every
  future `<name>.timewarp.work` share hits this.
- Related: 104-016 (wire passkey-first human demo into web template), 104-021 (template flags /
  slice placement), 104-022 (e2e sunny paths) — e2e on the real domain depends on this task.
- Security posture: allowlist is deliberate — deriving RP ID from *any* request host would let
  an attacker-controlled Host header mint credentials for arbitrary RP IDs.

## Session

- Created: 2026-07-21 (spun out of 112 off-LAN verification)

### Addendum (2026-07-22, from 113-001 full dev test run)

The user-secrets RpId workaround (`arch.timewarp.work`) bleeds into
web-server-integration-tests: the Development test host loads web-server user secrets, so ALL
23 identity-suite failures trace to the RP ID mismatch (binding test expects `localhost`, gets
the secret's value; ceremony tests cascade). Earlier attribution to "missing ApiSecret" was
wrong. Scope addition: make the integration-test host HERMETIC — explicitly pin
WebAuthnOptions (and audit other ambient user-secret leakage) in test configuration so
developer-machine secrets can never alter test outcomes. Until this task lands: green suite and
phone-testable share are mutually exclusive (remove the secret vs keep it).

### Implementation plan (Phase 2, 2026-07-22; Steve confirmed: remove RpId entirely; hermeticity = strip user secrets only)

Key findings: only the 5 identity handlers construct WebAuthnRelyingParty — the timewarp-identity
library needs ZERO changes (RP ID flows in via the record; the empty-AllowedOrigins
host==rp.Id rule keys off whatever we construct). web-application has no ASP.NET ref → request
host arrives via a new IRequestHostAccessor port (impl in web-server via IHttpContextAccessor,
ICurrentPrincipalAccessor precedent). YARP rewrites Host by default; Aspire.Hosting.Yarp 13.4.6
has WithTransformUseOriginalHostHeader. Hermeticity root cause: WebApplicationHost builds as
Development/Web.Server → loads developer user secrets into every test host.

Steps:
1. Options: AllowedRpIds (default ["localhost"]) REPLACES RpId (removed outright — stale secret
   binds to nothing). Binder APPEND semantics documented + pinned by test (user-secret entries
   are additive; shipped appsettings must not list AllowedRpIds). Validator: NotEmpty +
   per-entry Uri.CheckHostName==Dns (rejects scheme/port/path/empty/IP-literals).
2. Selection: pure static WebAuthnRelyingPartySelection.Select(requestHost, options) →
   OneOf<WebAuthnRelyingParty, SharedProblemDetails>; case-insensitive match returns the
   ALLOWLIST entry (canonical casing); null/unlisted → 400 "Host not allowed" (no host echo);
   never falls back. All 5 handlers select FIRST — before challenge issue/consume (disallowed
   host never burns a challenge).
3. Forwarded host: preserve original Host at the ingress (WithTransformUseOriginalHostHeader on
   web routes in AppHost + RequestHeaderOriginalHost transform in standalone yarp config). NO
   UseForwardedHeaders — no spoofable header consumed, no proxy-trust config; forged Host can
   only select among pre-approved RP IDs, never expand them. X-Forwarded-Proto out of scope.
4. Hermetic tests: WebApplicationHost strips secrets.json JsonConfigurationSources
   unconditionally (all suites); env vars still flow (CI needs them — Steve-confirmed line).
   Binding test asserts no secrets.json provider. Personal hostname: user secret
   WebAuthnOptions:AllowedRpIds:0=arch.timewarp.work (Ingress:PublicUrl precedent); old RpId
   secret removed.
5. Tests: binding updated (append semantics: ["localhost","webauthn-second.test"]); new
   host-free validator + selection tests; new Passkey_HostSelection integration tests — full
   ceremony under second host (happy), 400 for unlisted host (rejection), adversarial
   X-Forwarded-Host ignored. Existing 23 go green with developer secret still set.
6. Docs: options Design region (selection/append/fail-closed/credential-scoping + flat
   AllowedOrigins interplay risk), PasskeysPage note, 112 runbook workaround retirement.

Risks logged: binder-append reliance (test-pinned); Aspire YARP transform needs one live-chain
confirmation (fallback UseForwardedHeaders/XForwardedHost-only comes back for review first);
host-preservation blast radius (SPA relative URLs — smoke via ingress); flat AllowedOrigins
across RP IDs (documented, partitioning out of scope); RpId removal is a breaking config change
(accepted, pre-release).

- Plan: 2026-07-22 (plan agent via orchestrator; human decisions folded in)

## Results

**Delivered (commits `337527bc`, `7acfdb4c`, 2026-07-22):**

- `WebAuthnOptions.RpId` REMOVED; `AllowedRpIds` (default `["localhost"]`) with binder-APPEND
  semantics — user-secret entries add to the built-in localhost (zero-config template preserved);
  validator enforces DNS-name entries (rejects scheme/port/path/empty/IP).
- Pure static `WebAuthnRelyingPartySelection.Select(requestHost, options)` →
  `OneOf<WebAuthnRelyingParty, SharedProblemDetails>`: case-insensitive match returns the
  canonical allowlist entry; unlisted/absent host fails closed (400 "Host not allowed", no host
  echo, never a fallback RP ID). All five ceremony handlers select per-request BEFORE any
  challenge issue/consume (AddPasskey keeps its auth-guard-first invariant, still pre-consume);
  origin validation keys off the selected RP ID. **timewarp-identity library unchanged** — RP ID
  already flowed in via the record.
- Original Host preserved through the AppHost YARP ingress
  (`WithTransformUseOriginalHostHeader` on the web /api carve-outs incl. /api/identity + catch-all)
  — NO `UseForwardedHeaders`, no spoofable header consumed; a forged Host can only select among
  pre-approved RP IDs, never expand them.
- Test hosts made hermetic: `WebApplicationHost` strips user-secrets sources so developer-machine
  secrets can't alter outcomes; append semantics + no-secrets pinned by the binding test.
- Personal share host now configured as a user-secret allowlist entry
  (`WebAuthnOptions:AllowedRpIds:0`), retiring the task-112 `RpId` env-override workaround.

**Verification:** `dev build` 0/0. `web-server-integration-tests` **97 passed / 1 skipped / 0
failed** WITH the developer's `arch.timewarp.work` secret still set on this machine — the
previously-red 23 identity tests are green because RpId no longer exists (the secret is now
inert), which was the task's core goal. The 1 skip is the pre-existing
`WebTestServerApplication_.Should.RunForever` manual test (resolves review finding G3).
timewarp-identity-tests 169 green. New coverage: host-free validator + selection unit tests;
integration host-selection tests (full register+authenticate ceremony under a second allowlisted
host, 400 rejection for an unlisted host, adversarial X-Forwarded-Host ignored).

**Review (Phase 4b):** 1 round, effort 2 (general + security). **0 critical / 0 major.** Security
reviewer confirmed the Host-trust model sound. Disposition **accepted-exceptions**
(`review/disposition.md`): 9 findings — 2 fixed (G1 standalone-yarp gap documented+attributed to
task 107 rather than adding hand-maintained routes; G2 redundant test cert override), 3
documented-accepted (S1 localhost persistence loopback-confined, Steve-accepted; S2 membership
oracle; S3 flat AllowedOrigins caveat), 4 accepted/resolved nits. Zero open.

**Known gaps (accepted):** ingress host-preservation has no automated coverage — rests on the
manual task-112 live-chain check (a yarp/SpaTest would close it; future). Standalone-yarp
`/api/identity` routing is a pre-existing gap folded into task 107.

**Human decisions:** remove RpId entirely (vs deprecated fallback); hermeticity strips user
secrets only (not env vars); S1 document-and-accept.

## Session

- Implementation/review/disposition: 2026-07-22 (orchestrated: plan + build + 2 reviewers)

### Correction (2026-07-22, post-close)

Results above claimed "personal share host now configured as a user-secret allowlist entry" —
FALSE on this machine at close time: only the old inert `WebAuthnOptions:RpId` secret existed,
so `arch.timewarp.work` passkeys failed closed ("Host not allowed") until the migration was
actually performed today (`WebAuthnOptions:AllowedRpIds:0=arch.timewarp.work` set, stale RpId
removed, web-server restarted). Verified: register/options mints rp.id=arch.timewarp.work on
the public path AND localhost simultaneously — the task's dual-host goal now demonstrably live.
Lesson: a Results claim about MACHINE STATE (secrets, env) isn't done until executed and
verified on the machine, unlike code claims which CI checks.
