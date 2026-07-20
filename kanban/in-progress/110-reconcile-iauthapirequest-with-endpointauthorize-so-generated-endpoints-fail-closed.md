# Reconcile IAuthApiRequest with EndpointAuthorize so generated endpoints fail closed

## Description

Surfaced reviewing task 109 (web-server MVC → generated FastEndpoints). The FastEndpoint generator
honors only `[EndpointAuthorize]`; it reads `IAuthApiRequest`/`[AuthApiRequest]` **zero times**. An
unannotated contract generates `AllowAnonymous()` — a **fail-open** default. The repo has two
disconnected auth-intent vocabularies that don't talk:

- `IAuthApiRequest` / `[AuthApiRequest]` — the older, skill-documented marker (client attaches the
  token; `AuthApiRequestValidator` runs) — generator-blind.
- `[EndpointAuthorize]` — the new server-side marker the generator emits from.

**Concrete consequence today:** seven contracts declare `IAuthApiRequest` — including admin role
**create/update/delete/get/get-all** — and now generate as public `AllowAnonymous()` endpoints
whose own contract says they require auth. A contract author following the documented
`IAuthApiRequest` pattern who doesn't also know to add `[EndpointAuthorize]` ships a public
endpoint, silently.

**Not a live regression** — the old MVC shims carried no `[Authorize]`, and web-server had no
`UseAuthentication` until 104-003, so these were server-anonymous before and after. But it cements
a contradiction into generated code and bakes "admin role CRUD is public" into a template others
copy. It also violates task 109's own acceptance criterion: *"the contract must become the single
source of auth intent, not a hand-maintained sidecar."* The 109 review marked disposition clean
without cross-checking `IAuthApiRequest` contracts against generated auth — this task closes that
gap.

## Requirements

- **Single source of auth intent.** Decide the reconciliation at plan (both viable; not mutually
  exclusive):
  - **Generator honors `IAuthApiRequest`**: a contract implementing `IAuthApiRequest` (or carrying
    `[AuthApiRequest]`) generates a non-anonymous endpoint. Requires deciding the default scheme/
    policy when only `IAuthApiRequest` is present (no explicit policy) — likely "require
    authenticated user, any registered scheme" unless `[EndpointAuthorize]` refines it. Where both
    are present, `[EndpointAuthorize]` wins (it is the more specific, server-facing statement).
  - **TWA analyzer guard (prefer-analyzers directive — strongly recommended regardless):** flag any
    contract whose generated endpoint would be `AllowAnonymous` while it declares `IAuthApiRequest`
    (or, stricter: flag ANY `[ApiEndpoint]` contract that declares neither explicit
    `[EndpointAuthorize]` nor an explicit anonymous opt-out — force every endpoint to state its auth
    posture). Turns the fail-open default into a build break.
- **Explicit anonymous opt-out.** Anonymous must be a *stated* choice, not a silent default — e.g.
  an `[EndpointAllowAnonymous]` marker (or `[EndpointAuthorize]`'s absence only permitted when an
  explicit anonymous marker is present). The identity ceremony endpoints (register/token options +
  complete, passkey ceremonies, get-current-session) are legitimately anonymous and must carry the
  explicit opt-out so the analyzer passes and intent is visible.
- **Fix the seven live contracts**: admin roles CRUD + `get-sign-in-token` + `get-current-user` —
  give them real auth (`[EndpointAuthorize]` with the appropriate policy/scheme) OR an explicit
  anonymous opt-out with a documented reason. Do NOT leave them generated-anonymous-by-omission.
  (Roles CRUD almost certainly wants a real admin policy — coordinate with whatever role/authz the
  template intends; if no admin policy exists yet, that is its own scoping decision to record.)
- **Deduplicate the vocabularies going forward**: document (skill + AGENTS.md) which marker is
  canonical for server auth so authors aren't choosing between two. If `IAuthApiRequest` remains
  the client-facing signal, state explicitly that it alone does NOT secure the server endpoint and
  the analyzer enforces the pairing.
- **Tests**: analyzer positive/negative (IAuthApiRequest-without-endpoint-auth flagged; explicit
  anonymous passes; EndpointAuthorize passes); integration test that an `IAuthApiRequest` endpoint
  actually rejects an unauthenticated request (the roles endpoints — proving the fix is real, not
  just annotation).

## Checklist

- [ ] Plan: generator-honors-IAuthApiRequest vs analyzer-guard vs both; default-scheme decision
- [ ] Explicit anonymous opt-out marker; identity ceremony endpoints annotated with it
- [ ] Generator and/or analyzer change; fail-open default eliminated
- [ ] Seven live IAuthApiRequest contracts given real auth or documented explicit-anonymous
- [ ] Vocabulary canonicalized in skill + AGENTS.md
- [ ] Analyzer tests + integration test proving an IAuthApiRequest endpoint rejects anon
- [ ] dev build 0/0; full dev test

## Notes

### Implementation plan (2026-07-20)

#### Verified current state (evidence)

- `BuildAuthConfiguration` + `EndpointMetadata.FromSymbol`: no `[EndpointAuthorize]` → `AllowAnonymous();`. `IAuthApiRequest`/`[AuthApiRequest]` read zero times. Fail-open confirmed.
- The "seven," precisely: Roles CRUD ×5 (`create/update/delete-role`, `get-role` manual `IAuthApiRequest`; `get-roles` via `[AuthApiRequest]` mixin) — all `[ApiEndpoint]`, all generate `AllowAnonymous()` today. `get-sign-in-token`: `[ApiEndpoint]` but does NOT implement `IAuthApiRequest` (pre-auth ceremony per its own Design region); mints a Passwordless token for arbitrary caller-supplied UserId; no SPA consumer — dormant legacy. `get-current-user`: `[ClientOnlyContract]`, NO `[ApiEndpoint]` — outside the generator.
- Auth infra: `identity-session` cookie scheme (401/403 status codes), agent bearer + `agent-scope:identity:read` policy. NO session-authenticated policy, NO admin/role policy exists.
- Anonymous-today `[ApiEndpoint]` contracts with no marker: 13 in web-contracts (hello, track-event, get-profile, get-sign-in-token, get-current-session, 8 identity ceremonies) + api-contracts get-weather-forecasts. Only `get-agent-identity` carries `[EndpointAuthorize]`.
- TWA0013/TWA0014 free. Convention analyzers match by simple name; test infra `CSharpAnalyzerTest<TAnalyzer, FixieVerifier>`.
- `Roles_Endpoint_Tests` use ScopedSender (in-process mediator, bypasses HTTP/auth) → endpoint auth does not break them. Passkey tests show how to mint a real `identity-session` cookie over HTTP.
- Generator tests assert `AllowAnonymous()` for unannotated contracts (lines 43, 109) — must flip.

#### Committed decisions

- **D1 — Explicit-marker-pair enforced by analyzer; generator does NOT derive auth from IAuthApiRequest.** Every `[ApiEndpoint]` contract carries exactly one of `[EndpointAuthorize]` / `[EndpointAllowAnonymous]`. IAuthApiRequest is a client/mock-mode payload signal (server must re-derive identity); implicit derivation would invent default policy + hide posture. `IAuthApiRequest` ⇒ must-not-be-anonymous becomes agreement check TWA0014.
- **D2 — `[EndpointAllowAnonymous(reason)]`** in timewarp-architecture-attributes; **reason is a required ctor arg** (mirrors ClientOnlyContract — "an unexplained opt-out is just the drift with paperwork").
- **D3 — Generator default flips fail-closed**: no marker → emit NOTHING (FE requires auth by default) — unreachable in clean builds (TWA0013) but a suppressed analyzer now fails closed. Both markers → EndpointAuthorize wins + TWA0014 flags.
- **D4 — Roles CRUD gets real auth**: new policy `identity-session-authenticated` (`IdentitySessionDefaults.AuthenticatedPolicy`; `AddAuthenticationSchemes(identity-session).RequireAuthenticatedUser()`). Roles = the canonical CRUD demo consumers copy — it must teach the protected pattern. Real admin/role policy = recorded future work (any authenticated principal may mutate demo roles; deliberate).
- **D5 — TWA0013 (missing posture) + TWA0014 (conflicting posture)**, one new analyzer, convention-analyzers package, Warning (build-break under warnings-as-errors).
- **D6 — Land 110 before 104-030** (same generator emission path); get-weather-forecasts gets the explicit anonymous marker now.

#### The seven — dispositions

| Contract | Disposition |
|---|---|
| CreateRole / UpdateRole / DeleteRole / GetRole / GetRoles | `[EndpointAuthorize(Policy = "identity-session-authenticated")]` (comment names server constant; GetRoles keeps `[AuthApiRequest]` mixin for mock-mode UserId) |
| GetSignInToken | `[EndpointAllowAnonymous("Pre-auth sign-in ceremony …")]` + Design note: legacy Passwordless path, arbitrary-UserId minting is a known hazard slated for 104-016/021 |
| GetCurrentUser | No change — ClientOnlyContract, outside generator/TWA0013; 104-016/021 own retirement |

#### Ordered work items

1. **New attribute** `endpoint-allow-anonymous-attribute.cs` (AttributeUsage Class, sealed, required `Reason` ctor arg, Purpose/Design). Update `endpoint-authorize-attribute.cs` Design region (absence rule changed).
2. **Generator fail-closed**: `endpoint-metadata.cs` `AllowAnonymous` default false; true only when EndpointAllowAnonymous present AND EndpointAuthorize absent. Generator Design region + XML doc + `fast-endpoint-source-generator.md` updated.
3. **Policy wiring**: `IdentitySessionDefaults.AuthenticatedPolicy = "identity-session-authenticated"`; program.cs `.AddPolicy(...)` after agent policy.
4. **Annotate every `[ApiEndpoint]` contract**: roles five → EndpointAuthorize; EndpointAllowAnonymous(reason) → hello, track-event, get-profile (static demo profile; real auth with 104-016/024), get-sign-in-token, get-current-session (reads ambient session), 8 identity ceremony commands (pre-auth by nature), api-contracts get-weather-forecasts.
5. **Analyzer** `endpoint-auth-posture-analyzer.cs` (pattern: endpoint-coverage-analyzer.cs): SymbolAction on NamedType with ApiEndpointAttribute. TWA0013 missing posture; TWA0014 conflict = (a) both markers, or (b) EndpointAllowAnonymous while nested Query/Command implements IAuthApiRequest (AllInterfaces, simple-name) or carries [AuthApiRequest]. Location = the [ApiEndpoint] attribute application. Register both in AnalyzerReleases.Unshipped.md.
6. **Tests**: analyzer suite (no-marker → 13; authorize-only clean; anonymous-only clean; both → 14; anonymous+IAuthApiRequest → 14; anonymous+[AuthApiRequest] → 14; non-ApiEndpoint clean). Generator tests: flip lines 43/109 to ShouldNotContain("AllowAnonymous"); add explicit-anonymous → AllowAnonymous(); both-markers → policy, no AllowAnonymous. Integration `Features/Admin/Roles/Roles_Authorization_Tests.cs`: anonymous GET/POST api/Roles → 401; passkey-ceremony cookie (copy IntegrationSoftwareAuthenticator pattern) → GET 200 seeded roles. Isolated HttpClient per test.
7. **Docs**: web-api-contracts SKILL (three-state truth table; canonical statement: IAuthApiRequest is client/mock-mode identity ONLY, does not secure the server; EndpointAuthorize is sole server-auth marker; TWA0014 enforces pairing; workflow/checklist/pitfalls updates). AGENTS.md TWA rows + range 0002–0014. ADR-0007 consequences line. HowToWrite_BFF_API_Contracts note.
8. **Verify**: dev build 0/0; full dev test.

#### Scope boundaries

No admin/role-based policy (future work, recorded). No api-server bearer (104-030). No legacy removal (104-016/021). No GetCurrentUser change. No SPA sign-in UX (real-mode roles pages will 401 until signed in — correct; mock unaffected). No AuthApiRequestValidator/[Page] changes. No new packages.

#### Open Questions

None.

- Origin: 2026-07-20 review of task 109. Evidence: generator
  `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
  `BuildAuthConfiguration` (no-attribute → `AllowAnonymous()`); `IAuthApiRequest` contracts under
  `web-contracts/features/admin/roles/**`, `auth/`, `authentication/`.
- Relates to 104-030 (api-server bearer wiring) — same generator, same auth-emission path; consider
  sequencing so the reconciliation lands before more endpoints are generated on api-server.
- ADR-0007 (endpoints are generated FastEndpoints on both servers) should gain a line on how auth
  intent is expressed once this is decided.

## Session

- Created: 2026-07-20
