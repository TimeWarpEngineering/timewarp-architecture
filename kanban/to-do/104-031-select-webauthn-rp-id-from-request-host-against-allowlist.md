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
