# Round 1 — security
**Date:** 2026-07-20
**Scope reviewed:** commit 44fd802f vs parent (security lens)

## Summary

The original 109 finding — admin Roles CRUD shipping as generated `AllowAnonymous()` while their
contracts declared `IAuthApiRequest` — is genuinely closed. The generator default is now fail-closed
(no marker → emit nothing → FastEndpoints requires auth), the five Roles contracts carry
`[EndpointAuthorize(Policy="identity-session-authenticated")]` which the generator turns into
`Policies("identity-session-authenticated")`, and a real-HTTP integration test proves anonymous
GET/POST `api/Roles` → 401 while a passkey session cookie → 200. I verified every one of the 20
`[ApiEndpoint]` contracts in the tree now carries exactly one posture marker (no gaps, no
double-applied markers), that both-markers precedence resolves to `[EndpointAuthorize]` in the
generator and is separately build-broken by TWA0014, and that the `identity-session-authenticated`
policy is scheme-restricted so an agent bearer token cannot satisfy it. Cross-scheme isolation from
104-004 survives. Three findings, none of which reopens the 109 gap: one pre-existing account-takeover
primitive that this diff formally blesses as public (get-sign-in-token), one missing negative test,
and one defense-in-depth note about the fail-closed challenge path.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/web-contracts/features/auth/queries/get-sign-in-token.cs:23 (and handler source/container-apps/web/web-application/features/auth/get-sign-in-token-handler.cs:33-40)
- Description: `[EndpointAllowAnonymous(...)]` now formally states that `api/signin-token` is a
  deliberately public endpoint. Its handler calls `PasswordlessClient.GenerateAuthenticationTokenAsync(new AuthenticationOptions(request.UserId))`
  — it mints a real sign-in verification token for an **arbitrary, caller-supplied `UserId`** with no
  server-side check that the caller is that user. Any anonymous caller can request a token for any
  `UserId` and redeem it through the Passwordless JS flow to sign in as that principal — an account-takeover
  primitive. This is not "dormant" in the sense of non-functional: `AddPasswordlessSdk` in program.cs
  throws at startup if no `Passwordless:ApiSecret` is configured, so any running instance has this
  endpoint fully wired and functional; "dormant" only means the SPA no longer calls it. This is a
  pre-existing hazard, NOT introduced by this diff, and task 110's plan (D-section "the seven",
  Scope boundaries) explicitly defers retirement to 104-016/104-021. I am flagging it because the
  act of annotating it `[EndpointAllowAnonymous]` converts a silent gap into a stated "this is
  intentionally public" — and the reachable vulnerability persists behind that stamp. The Design
  note is honest and names the hazard accurately, so this is a conscious-acceptance call, not a
  concealment.
- Suggestion: Neutralize it in this fail-closed pass rather than blessing it as anonymous — either
  drop `[ApiEndpoint]` so no HTTP surface is generated (mark `[ClientOnlyContract(reason)]` like
  get-current-user), or gate the handler to reject when the caller's authenticated principal id does
  not equal `request.UserId`. If the team prefers to keep the disposition as-is, that is defensible
  given the documented retirement path, but the acceptance should be explicit in triage rather than
  implied by the anonymous marker.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/Features/Admin/Roles/Roles_Authorization_Tests.cs:44-90
- Description: The new policy `identity-session-authenticated` is scheme-restricted
  (`AddAuthenticationSchemes(IdentitySessionDefaults.Scheme).RequireAuthenticatedUser()`, program.cs:116-122),
  so by construction an agent bearer token (scheme `agent-token`) cannot satisfy it — the 104-004
  scheme-isolation property. The tests prove the positive (cookie → 200) and the unauthenticated
  negative (anonymous → 401), but there is no test proving the **cross-scheme** negative: a valid
  agent bearer token presented to `api/Roles` must still be rejected. 104-004 established scheme
  isolation with a dedicated test; this diff introduces a second consumer of that property without a
  covering test, so a future regression (e.g. someone adding `agent-token` to the policy's scheme
  list, or making it the default) would not be caught here.
- Suggestion: Add one test — mint an agent token (the agent-token issuance ceremony is already
  exercised in the identity integration suite), send `GET api/Roles` with the bearer header, assert
  401/403. Belt-and-suspenders proof that the cookie policy and the agent policy stay disjoint.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/web-server/program.cs:199-234 (ConfigureAuthentication) vs the fail-closed default
- Description: The fail-closed claim ("no marker → FE requires auth by default") is correct and means
  **deny**, not bypass — verified. But note the challenge path for a hypothetical no-marker endpoint:
  the generator emits no `AuthSchemes(...)`, so FastEndpoints' default authorization requirement uses
  the app's **default** authentication/challenge scheme, which `AddMicrosoftIdentityWebAppAuthentication`
  registers (the "dormant Entra" OIDC/cookie pair), NOT the named `identity-session` cookie scheme. So
  such an endpoint would deny even a valid identity-session cookie holder (correct — fail closed) but
  would challenge an anonymous caller via Entra OIDC (a 302 to a login that isn't wired, or a 500 if
  the OIDC handler is misconfigured) rather than the clean 401 that the scheme-restricted Roles policy
  produces via `OnRedirectToLogin`. This is unreachable in a clean build (TWA0013 build-breaks any
  no-marker `[ApiEndpoint]`), so it only matters under a suppressed analyzer — and even then the
  property that counts (no anonymous access) holds. Recording it only so the "fail-closed = clean 401"
  intuition isn't over-generalized: clean 401 is a property of the explicit scheme restriction on the
  policy, not of the bare fail-closed default.
- Suggestion: None required. If defense-in-depth against a suppressed TWA0013 is ever wanted, the
  generated no-marker path could emit `AuthSchemes(IdentitySessionDefaults.Scheme)` as a
  last-resort default, but that reintroduces a hardcoded scheme assumption the generator otherwise
  avoids — not worth it given TWA0013.
- Status: open

## Verification notes (adversarial checklist — no finding, for the record)

- **Fail-closed at runtime**: No global FastEndpoints security fallback exists — `AddFastEndpoints`
  (program.cs:151-162) sets only `IncludeAbstractValidators=false`, `DisableAutoDiscovery`,
  `Assemblies`; no `Endpoints.Configurator` re-enabling anonymous. Generator no-marker test asserts
  `ShouldNotContain("AllowAnonymous")`. FE's secured-by-default behavior therefore governs.
- **Both-markers precedence**: `EndpointMetadata.FromSymbol` checks `endpointAuthorize` first and
  leaves `AllowAnonymous=false` (endpoint-metadata.cs:100-115); generator test
  `Should_Prefer_EndpointAuthorize_When_Both_Markers_Present` asserts `Policies("AdminOnly")` and no
  `AllowAnonymous`. TWA0014(a) build-breaks the combination. Belt matches suspenders. Confirmed.
- **Roles policy scheme isolation**: `AddAuthenticationSchemes(IdentitySessionDefaults.Scheme)` on the
  policy means only the cookie scheme authenticates; agent token cannot satisfy it. Reverse
  (`agent-scope:identity:read`) unchanged — still `AddAuthenticationSchemes(agent-token)` +
  `RequireClaim`, which a cookie cannot meet. Confirmed (test gap noted as Issue 2).
- **The 14 anonymous grants**: reviewed each reason against behavior. `get-profile` is safe —
  `CurrentUserService.UserId is null → return mock`; an anonymous caller cannot supply a `UserId` to
  read another user's synthesized data (id is derived server-side from session), so no info
  disclosure; avatar is fetched server-side and embedded, no id leak to the third party. `get-current-session`,
  the 8 identity ceremonies, `hello`, `get-weather-forecasts` are pre-auth/public by nature.
  `track-event` is an unauthenticated write (spam/abuse surface, no rate limit) but that is an
  intentional analytics design, not a posture error; the payload carries no PII as claimed.
  `get-sign-in-token` is Issue 1.
- **Analyzer evasion (TWA0013/0014)**: attribute matching is by simple name for BOTH the generator and
  the analyzer, so they are symmetric — a contract cannot be generated as an endpoint yet dodge the
  analyzer by naming. A same-simple-name attribute from elsewhere would over-trigger (false positive),
  not dodge; a `using`-alias does not change `AttributeClass.Name`, so it cannot dodge. Partial classes:
  `RegisterSymbolAction` sees the merged `INamedTypeSymbol`, whose `GetAttributes()` aggregates all
  partial declarations — a marker on any partial file counts, so split-file evasion is not possible.
  `ConfigureGeneratedCodeAnalysis(None)` does not skip these contracts (the primary declaration is
  hand-written). TWA0014(b) matches `IAuthApiRequest` via `AllInterfaces` AND the `[AuthApiRequest]`
  mixin attribute independently, covering pre- and post-mixin-generation compilation states. The
  request-type lookup keys on nested `Query`/`Command`, matching the generator's own `RequestTypeName`
  constraint, so an auth-intent request under a different name could not be routed by the generator
  either. No evasion path found.
- **Roles anon 401 is genuine auth**: `UseAntiforgery` (program.cs:268) is Blazor-only and not applied
  to FE JSON APIs (FE antiforgery is not enabled), so the anonymous POST's 401 is an authorization
  challenge, not an antiforgery/binding 400. Confirmed.

## Result

Two open findings of note plus one nit: **1 bug** (Issue 1 — get-sign-in-token remains a reachable
arbitrary-`UserId` sign-in-token minter; pre-existing, honestly documented, deferred to 104-016/021 by
the plan — flagged so the acceptance is conscious, not a 110 regression), **1 suggestion** (Issue 2 —
missing cross-scheme negative test that an agent token cannot satisfy the roles cookie policy), **1 nit**
(Issue 3 — the bare fail-closed default challenges via the dormant Entra default scheme, unreachable
under TWA0013; deny still holds). Verdict: the original 109 finding — admin CRUD generated-anonymous —
is **genuinely closed**: every generated endpoint in the tree now has a stated posture, roles CRUD is
policy-protected end-to-end with a real-HTTP test proving 401/200, and the fail-open default is
eliminated in both the generator and the two build-breaking analyzers. None of the three findings
reopens that gap.
