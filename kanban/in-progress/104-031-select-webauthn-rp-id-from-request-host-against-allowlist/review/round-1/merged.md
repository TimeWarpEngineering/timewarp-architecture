# Round 1 — merged findings (general + security, effort 2)

**0 critical / 0 major from both reviewers.** Host-trust model verified sound (forged Host can
only select an already-approved RP ID, never expand the allowlist; browser origin + rpIdHash
binding blocks completion; canonical-casing return is a real safety property). Fail-closed
complete (one gated `new WebAuthnRelyingParty`, all 5 handlers select before challenge burn).
Hermetic strip closes a real vector with zero blast radius.

| id | sev | status | finding | disposition |
|----|-----|--------|---------|-------------|
| G1 | minor | open | standalone yarp `RequestHeaderOriginalHost` on WebRoute/WebSwaggerRoute doesn't reach passkeys: `/api/identity/*` matches the pre-existing `ApiRoute` catch-all → Api.Server. Fuller review: this is a PRE-EXISTING gap (standalone yarp never routed identity to Web.Server), NOT a 104-031 regression; the transform added is harmless; the AppHost path (task-112, verified) is correct. | FIX (minimal, no scope creep): keep the WebRoute/WebSwaggerRoute transforms (correct for those routes); correct only the false "standalone yarp covers passkey host" claim; note the pre-existing identity-routing gap relates to task 107 (yarp route generation), not fixed here. |
| S1 | minor | open | AllowedRpIds append-only → `localhost` can never be removed in prod; empty-AllowedOrigins fallback permanently accepts rp.id=localhost (confined to loopback self-registration, not remotely exploitable) | DOC: options Design region — accepted for the template default; note the loopback-confined posture. Steve-flagged. |
| S2 | minor | open | anonymous Start endpoints are an allowlist-membership oracle (200 vs 400) | DOC: scope the "no enumeration" claim to "no host reflected"; membership of public hostnames is low-value |
| S3 | minor | open | flat AllowedOrigins across all RP IDs = footgun if populated in multi-RP prod (blocked one layer down by browser rpId-suffix + rpIdHash) | DOC: already noted in options Design region as out-of-scope; confirm wording covers the multi-RP risk |
| G2i | nit | open | ingress host-preservation has no automated coverage (host-selection tests hit web-server directly); rests on manual live-chain check | ACCEPT: record as known gap in Results |
| G2 | nit | open | Passkey_HostSelection_Tests SNI/dev-cert comment overstates — Headers.Host doesn't change TLS SNI (URI authority does); DangerousAcceptAnyServerCertificateValidator likely redundant | FIX: drop the redundant cert override or correct the comment (test-only; outcome unaffected) |
| S4 | nit | open | AllowedHosts:"*" leaves per-request allowlist as sole host gate | ACCEPT: acceptable; optional defense-in-depth |
| S5 | nit | open | hermetic strip exact-matches Path=="secrets.json" (framework-fragile) | ACCEPT: pinned by binding test — fails loud if framework changes |
| G3 | nit | n/a | commit's "97/1/0" ordering ambiguous (skip vs fail) | RESOLVE in Results: the 1 is the pre-existing WebTestServerApplication_.Should.RunForever manual skip |
| G4 | nit | n/a | host-free unit tests in integration project | ACCEPT: matches existing grouping precedent |

Plan: FIX G1 (correctness); DOC S1/S2/S3 (Design-region hardening notes); ACCEPT G2/S4/S5/G4;
RESOLVE G3 in Results with the explicit skip identity. Round-2 = orchestrator diff + targeted
build.
