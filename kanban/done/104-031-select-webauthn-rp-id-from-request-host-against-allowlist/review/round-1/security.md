# Security review — 104-031 (round 1)

Commit `337527bc`: per-request WebAuthn RP ID selection from the request Host against an
`AllowedRpIds` allowlist; original-Host preservation through the ingress; static `RpId` removed.

Adversarial mindset: I tried to make a forged Host mint or use a credential for an unintended RP
ID, bypass the fail-closed gate, burn a challenge on a rejected host, slip a bad entry past the
validator, replay a ceremony across hosts, and leak the allowlist. **No critical or major findings.**
The Host-trust model is sound: the allowlist is a real boundary, the forwarded-header surface is not
consumed, and the browser's origin/`rpIdHash` binding backstops every case where a forged Host could
select a different (but still allowlisted) RP ID. Findings below are all minor/nit hardening notes.

---

## What I tried to break and found SOLID

- **Forged `Host` / `X-Forwarded-Host` reaching an unintended RP ID.** `HttpRequestHostAccessor`
  reads `HttpContext.Request.Host.Host` only; there is no `UseForwardedHeaders`/`ForwardedHeaders`
  registration anywhere in `source/` (grepped), so `X-Forwarded-Host` is inert. A forged `Host` can
  at most SELECT an already-approved entry, never expand the allowlist. Even selecting a *different*
  allowlisted RP ID gains nothing: to complete a ceremony the attacker still needs a browser at that
  origin (clientData.origin host must equal the selected Id — `webauthn-origin.cs:26-27`) and a
  signature over `authenticatorData` carrying `SHA256(selected Id)` (`webauthn-registration.cs:111-113`,
  `webauthn-authentication.cs:74-76`). Both are enforced against the operator-approved canonical Id,
  not the request's. Solid.
- **Attacker-controlled casing echoed into the RP ID.** `Select` matches `OrdinalIgnoreCase` but
  returns the **allowlist entry's** casing, never the request's (`web-authn-relying-party-selection.cs:39-43`).
  So `rpIdHash`/options always use the operator's spelling. This is a genuine safety property, not
  cosmetic — verified by `WebAuthnRelyingParty_Selection_Tests.Return_Canonical_Allowlist_Entry...`.
- **Fail-closed completeness.** Exactly one `new WebAuthnRelyingParty` in the whole tree, inside the
  gated `Select` (grep confirmed). All five WebAuthn ceremony handlers call `Select` as their first
  ceremony step; the agent-key handlers (`*agent*.cs`) run a different ceremony and never construct a
  WebAuthn relying party (grep confirmed — no `rpId`/RP references). No path mints a relying party
  or runs `WebAuthnRegistration/Authentication.Verify` without going through `Select`.
- **Challenge burn on a rejected host.** In all four Start/Complete handlers `Select` runs BEFORE
  `ChallengeStore.Issue`/`TryConsume`; in `AddPasskey` it runs after the auth guard but still before
  any challenge consume. A disallowed host returns 400 without touching challenge state. Verified by
  reading each handler; ordering matches the Design regions.
- **Validator bypass (punycode / trailing-dot / case / wildcard / IP / empty / null).** I ran
  `Uri.CheckHostName` against the tricky inputs: `*.timewarp.work` → `Unknown` (rejected),
  `127.0.0.1`/`::1`/`0`/`999999` → IP types (rejected), `""`/` localhost`/`localhost ` → `Unknown`
  (rejected), `localhost.`/`arch.timewarp.work.` (trailing dot) and `xn--…`/`LOCALHOST` → `Dns`
  (accepted). Trailing-dot and punycode entries are accepted BUT the runtime match is exact
  `OrdinalIgnoreCase`, so they can only ever **over-reject** (a `localhost.` entry won't match a
  `localhost` request) — never over-accept. `ValidateOnStart()` (`program.cs:345-346`) means an empty
  or non-DNS `AllowedRpIds` crashes boot. Fail-closed throughout.
- **Cross-host challenge replay.** The challenge store keys entries by the challenge bytes and is not
  RP-ID-scoped (`in-memory-webauthn-challenge-store.cs`), so a challenge issued under host A can be
  consumed by a completion under host B (same ceremony type). This grants nothing: the completion's
  `Verify` binds the selected Id, so the attestation/assertion must carry origin B and `SHA256(B)`,
  which only a browser actually at origin B (or the victim's private key) can produce — neither of
  which a cross-host replay supplies. Registration is open self-service anyway, so self-registering a
  credential under a host you already control is not a privilege gain.
- **Cross-RP-ID credential use (S6 in the brief).** `Credential` is scoped by `PrincipalId`, not by
  RP ID; authentication looks a credential up by handle then `Verify` binds the selected Id's
  `rpIdHash` into the signed data. A credential registered under RP ID A cannot produce a valid
  assertion under RP ID B without the private key, and the authenticator won't sign B's `rpIdHash`
  for an A-scoped credential. So no cross-RP-ID impersonation. List/revoke scope to the authenticated
  principal (`get-credentials-handler.cs:44-51`, `revoke-credential-handler.cs:105-121`), so a
  principal that spans multiple RP IDs sees/revokes all of its own credentials — benign (it is one
  account), see S3 note.
- **Hermetic secrets strip (S7 in the brief).** Stripping the `secrets.json` source removes a real
  vector (a developer's `AllowedRpIds` secret silently changing test outcomes) and does not create a
  vacuous-pass risk: the ceremony vectors in `Passkey_HostSelection_Tests` are built to match
  committed config (`webauthn-second.test`), so a test relying on a real secret would now FAIL, not
  pass. Env vars are intentionally left intact for CI. The `secrets.json` result is pinned by
  `WebAuthnOptions_Binding_Tests.NoUserSecrets_Source_Given_HermeticHost`.

---

## Findings

### S1 — Append-only `AllowedRpIds` makes `localhost` permanently un-removable; no replace/narrow path
- **Severity:** minor (hardening)
- **Status:** open
- **File:** `source/container-apps/web/web-application/configuration/web-authn-options.cs:34-49,71`
- **Description:** The binder-append semantics (C# default `["localhost"]` + config entries appended,
  not replaced) are the whole zero-config story, but the consequence is that a production deployment
  can never REMOVE `localhost` from the effective allowlist through configuration — only add to it.
  Combined with the empty-`AllowedOrigins` dev fallback ("accept any https origin whose host == the
  selected Id"), a public deployment permanently accepts `rp.id=localhost` ceremonies. I walked the
  impersonation path and it does NOT reach a victim: `localhost`-bound credentials only exist if a
  victim registered while their browser was at a `localhost` origin against THIS server (won't happen
  on a public host), and authenticating as such a principal still needs their private key. So the
  blast radius is confined to loopback self-registration — not remotely exploitable — but "the
  operator cannot restrict passkeys to only their real hostname" is a latent hardening gap that would
  matter if the origin fallback or credential scoping ever loosened.
- **Suggested fix:** Document explicitly in the Design region that `localhost` cannot be removed via
  config and why that is safe (loopback-bound); OR, if narrowing is ever wanted, provide a
  `ReplaceDefaultRpIds`/sentinel mechanism (e.g. a leading `"-"` or an explicit "replace" flag) so a
  production operator can pin the allowlist to exactly their host. No code change required for
  security now — this is a note so a future reader does not assume the allowlist is fully operator-controlled.

### S2 — Anonymous Start endpoints are an allowlist-membership oracle (400 "Host not allowed" vs 200)
- **Severity:** minor
- **Status:** open
- **File:** `source/container-apps/web/web-application/features/identity/web-authn-relying-party-selection.cs:45-51`;
  `start-passkey-registration-handler.cs`, `start-passkey-authentication-handler.cs`
- **Description:** The Design region markets "no host echo" (Detail does not reflect the requested
  host) as the anti-enumeration property, and that part is correct. But the response *shape* is itself
  the oracle: on the anonymous StartRegistration/StartAuthentication endpoints, an allowlisted Host
  returns 200 with options while a non-allowlisted Host returns 400 "Host not allowed". An attacker
  can confirm whether a guessed hostname is in the allowlist by flipping the `Host` header. Impact is
  low — allowlist entries are public DNS names the service answers on, and reaching web-server with an
  arbitrary Host generally requires getting past the SNI/host-routing ingress — but a developer's
  personal share host added via user secret (the documented `arch.timewarp.work` pattern) is at least
  confirmable. Timing adds nothing beyond the status-code distinction.
- **Suggested fix:** Accept as low-risk and note it in the Design region so the "no enumeration"
  claim is scoped to "no host reflected," not "membership unobservable." If membership must be opaque,
  the Start endpoints would need to return a uniform 200-with-generic-options (or a uniform 400) for
  unlisted hosts — likely not worth it for public hostnames.

### S3 — Flat `AllowedOrigins` shared across all RP IDs (documented caveat) has no guard
- **Severity:** minor
- **Status:** open
- **File:** `source/container-apps/web/web-application/configuration/web-authn-options.cs:50-57`;
  `web-authn-relying-party-selection.cs:43`
- **Description:** `AllowedOrigins` flows unpartitioned onto every selected relying party. The Design
  region flags this as out of scope. In the default empty state it is safe (the host-equals-selected-Id
  fallback re-scopes per request). If an operator populates `AllowedOrigins` explicitly AND serves
  multiple RP IDs, `WebAuthnOrigin.IsAllowed` switches to exact-list matching that ignores the
  selected Id (`webauthn-origin.cs:19-22`), so a listed origin is accepted for every RP ID. I tried to
  turn this into a cross-RP-ID confusion attack and it is blocked one layer down: the browser only
  emits `rpIdHash` for an RP ID that is a registrable suffix of the page origin's domain, and the
  server re-checks `rpIdHash == SHA256(selected Id)`, so a real browser cannot present origin-B with
  `rpIdHash`-A. So this is theoretical, not currently exploitable — but it is an un-guarded footgun.
- **Suggested fix:** Either add a validator note/rule when `AllowedOrigins` is non-empty alongside
  more than one `AllowedRpIds` entry, or (cleaner, future) partition origins per RP ID. At minimum the
  caveat is adequately documented; flag if multi-RP-with-explicit-origins ever ships.

### S4 — `AllowedHosts: "*"` leaves the per-request allowlist as the sole host gate
- **Severity:** nit (defense-in-depth)
- **Status:** open
- **File:** `source/container-apps/web/web-server/appsettings.json:19`
- **Description:** With `AllowedHosts: "*"`, Kestrel/HostFiltering accepts any `Host`, so every
  request reaches the handler and the WebAuthn `AllowedRpIds` allowlist is the only thing constraining
  host-derived behavior. That is acceptable (the allowlist IS the boundary, and this is a template
  default), but there is no host-filtering defense-in-depth beneath the feature-level gate.
- **Suggested fix:** None required. Optionally note that production deployments may also set
  `AllowedHosts` to the real hostname(s) as a belt-and-suspenders layer independent of the passkey
  allowlist.

### S5 — Hermetic strip matches `JsonConfigurationSource.Path == "secrets.json"` by exact equality
- **Severity:** nit
- **Status:** open
- **File:** `tests/common/timewarp-testing/web-application-host.cs:56-63`
- **Description:** The strip identifies the user-secrets source by `jsonSource.Path == "secrets.json"`.
  This is correct for the current `AddUserSecrets` provider, but is framework-version-fragile — a
  future provider path change would silently stop stripping, re-opening the "developer secret alters
  test outcome" vector. There is no security downside to the strip itself. The result is pinned by the
  binding test asserting no source path ends with `secrets.json`, so a regression fails a test rather
  than passing silently — which adequately mitigates the fragility.
- **Suggested fix:** None strictly needed given the pin. Optionally match on the provider TYPE
  (user-secrets provider) rather than the literal path string for robustness.

---

## Summary (count by severity)

- critical: 0
- major: 0
- minor: 3 (S1, S2, S3)
- nit: 2 (S4, S5)

Core Host-trust model, fail-closed completeness, challenge lifecycle, validator, and cross-RP-ID
credential scoping all verified solid. The remaining items are hardening/documentation notes, none
blocking.
