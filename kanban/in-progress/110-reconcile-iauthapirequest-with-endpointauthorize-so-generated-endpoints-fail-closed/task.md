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

- [x] Plan: generator-honors-IAuthApiRequest vs analyzer-guard vs both; default-scheme decision
- [x] Explicit anonymous opt-out marker; identity ceremony endpoints annotated with it
- [x] Generator and/or analyzer change; fail-open default eliminated
- [x] Seven live IAuthApiRequest contracts given real auth or documented explicit-anonymous
- [x] Vocabulary canonicalized in skill + AGENTS.md
- [x] Analyzer tests + integration test proving an IAuthApiRequest endpoint rejects anon
- [x] dev build 0/0; full dev test

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

## Results

### Summary

Implemented D1–D6 exactly as planned. The generator's fail-open default (no marker →
`AllowAnonymous()`) is now fail-closed (no marker → nothing emitted, so FastEndpoints' own
"auth required" default applies), enforced going forward by two new analyzers (TWA0013/TWA0014).
All 20 existing `[ApiEndpoint]` contracts were annotated with an explicit posture marker in the
same pass as the generator flip, so no intermediate broken state existed. Roles CRUD (the "seven"
minus `get-sign-in-token`/`get-current-user`) got real auth via a new
`identity-session-authenticated` policy; the remaining 14 previously-anonymous-by-omission
contracts got `[EndpointAllowAnonymous(reason)]` with an individually-written, verified-against-the-
handler reason (one draft reason on `get-profile` was factually wrong on first pass — corrected
after reading the handler).

A genuine pre-existing gap in test coverage was also discovered and closed: `Roles_Endpoint_Tests`
used `.Send()` (in-process mediator, bypasses ASP.NET Core's auth pipeline entirely), so it could
never have caught the fail-open bug and still can't after the fix. New `Roles_Authorization_Tests`
uses real HTTP with isolated `HttpClient`s to prove the fix is real: anonymous GET/POST 401,
passkey-session-cookie GET 200 with seeded roles.

### Work items

1. **New attribute + Design region.** `endpoint-allow-anonymous-attribute.cs` (new, sealed,
   `AttributeUsage(Class)`, required `Reason` ctor arg — mirrors `ClientOnlyContractAttribute`).
   `endpoint-authorize-attribute.cs` Design region updated: absence no longer means anonymous.
2. **Generator fail-closed.** `endpoint-metadata.cs`: `AllowAnonymous` defaults unset/false;
   `FromSymbol` sets it `true` only when `EndpointAllowAnonymous` is present and `EndpointAuthorize`
   is absent. `BuildAuthConfiguration` itself needed no logic change — its existing
   `lines.Count > 0 ? ... : string.Empty` fallback already produced fail-closed emission once the
   metadata determination was fixed. `fast-endpoint-source-generator.md` gained an Authorization
   section documenting the new default.
3. **Policy wiring.** `IdentitySessionDefaults.AuthenticatedPolicy = "identity-session-authenticated"`;
   `program.cs` adds a second `.AddPolicy(...)` (`AddAuthenticationSchemes(identity-session)
   .RequireAuthenticatedUser()`) alongside the existing agent-token policy. Deliberately not an
   admin/role policy — recorded as a known simplification ("any authenticated principal may mutate
   demo roles").
4. **All 20 `[ApiEndpoint]` contracts annotated** — 5 roles (`CreateRole`/`UpdateRole`/`DeleteRole`/
   `GetRole`/`GetRoles`) → `[EndpointAuthorize(Policy = "identity-session-authenticated")]`
   (`GetRoles` kept its `[AuthApiRequest]` mixin for mock-mode `UserId`); 14 →
   `[EndpointAllowAnonymous(reason)]` with an individual honest reason each (hello, track-event,
   get-profile, get-sign-in-token, get-current-session, 8 identity ceremony commands,
   api-contracts get-weather-forecasts); `get-agent-identity` already had `[EndpointAuthorize]` from
   104-004 (unchanged); `get-current-user` unchanged (`[ClientOnlyContract]`, outside the generator).
   **Note:** by the time this task ran, 109 had already replaced all hand-written endpoint shims
   with pure contract annotation — there were no endpoint class files to touch, only contracts.
5. **New analyzer** `endpoint-auth-posture-analyzer.cs` (`EndpointAuthPostureAnalyzer`, TWA0013 +
   TWA0014, `RegisterSymbolAction` on `NamedType`, pattern-matched from `EndpointCoverageAnalyzer`).
   TWA0014(b) detects `IAuthApiRequest` on the nested `Query`/`Command` via **both** signals
   independently (`AllInterfaces` simple-name match, and `[AuthApiRequest]`-mixin attribute
   simple-name match) since the mixin generator produces both simultaneously and either could be
   the visible form at a given contract's compilation stage. Registered in
   `AnalyzerReleases.Unshipped.md`.
6. **Tests.** Analyzer suite: 7 new tests (no-marker/TWA0013, authorize-only clean, anonymous-only
   clean, both-markers/TWA0014, anonymous+manual-`IAuthApiRequest`/TWA0014, anonymous+mixin-
   attribute/TWA0014, non-`[ApiEndpoint]` clean) — both TWA0014(b) shapes explicitly exercised per
   the dispatch's caution. Generator suite: 2 existing assertions flipped
   (`ShouldContain("AllowAnonymous()")` → `ShouldNotContain("AllowAnonymous")`), 2 new tests added
   (explicit-anonymous emits `AllowAnonymous()`; both-markers emits the policy and no
   `AllowAnonymous`). Integration: new `Roles_Authorization_Tests.cs` (3 tests, real HTTP, isolated
   `HttpClient`s, real passkey-ceremony cookie — copied `Passkey_Registration_Tests`' minting flow
   rather than reusing it, matching that file's own duplication rationale). Also fixed a regression
   this task's own change exposed in `CreateRole_Endpoint_Tests.cs` (see Deviations below).

### Docs (work item 7)

- `skills/web-api-contracts/SKILL.md`: FastEndpoint-generation table now lists both markers plus
  the fail-closed "neither" row; "Auth requests — two forms" section rewritten as "Auth requests vs.
  server auth" with the canonical statement (`IAuthApiRequest` is client/mock-mode identity only,
  does not secure the server; `[EndpointAuthorize]` is the sole server-auth marker) and a
  four-row truth table (three valid states + the TWA0014-forbidden one); workflow scaffold step,
  validation checklist, and pitfalls table all updated; `when-to-use` keywords gained
  `EndpointAllowAnonymous`.
- `AGENTS.md`: server-endpoints stack bullet, key-patterns bullet, TWA table (+TWA0013/0014 rows),
  package-table range (TWA0002–0012 → TWA0002–0014), Definition-of-Done bullet.
- `timewarp-architecture-convention-analyzers.csproj` `<Description>`: range extended to match.
- ADR-0007: Decision Outcome's auth-marker line rewritten for both markers + fail-closed; new
  Negative Consequences bullet naming task 110; Links' task list gained "110 (fail-closed auth
  default)".
- `documentation/developer/reference/ApiEndpointSourceGenerator.md` (the separate generator
  reference doc, not `fast-endpoint-source-generator.md`): usage example now shows
  `GetWeatherForecasts` carrying its real `[EndpointAllowAnonymous(reason)]`; authorization table
  rewritten with the fail-closed row and TWA0013/0014; Customization and Best-practices sections
  updated; Diagnostics section note extended to mention TWA0013/0014.
- `HowToWrite_BFF_API_Contracts.md`: added the canonical
  IAuthApiRequest-does-not-secure-the-server statement with a pointer to the skill's truth table.

### Deviations from the plan (with rationale)

- **`CreateRole_Endpoint_Tests.cs` regression, not anticipated by the plan.** Once roles required
  auth, `ValidationError_Given_Empty_Name`/`_Given_Empty_UserId` (both use
  `ConfirmEndpointValidationError`, real HTTP) started 401ing before FluentValidation ran; the third
  test in the same file (`.Send()`, in-process, bypasses auth) was unaffected. Fixed by adding an
  `EnsureAuthenticatedAsync()` helper that mints a passkey session through the same shared
  `WebTestServerApplication.HttpClient` the validation-error calls use, so the cookie lands in the
  ambient jar automatically — deliberately different from `Roles_Authorization_Tests`' isolated-
  client pattern, since this class's tests never assert on session state and cross-test cookie
  leakage isn't a concern here. Documented in a new Design region on the file.
- **`get-profile.cs` reason self-corrected before commit.** First draft claimed the handler
  "returns the same static demo profile for every caller" — false. Reading
  `get-profile-handler.cs` showed it's genuinely dual-mode (anonymous demo response vs.
  `UserId`-synthesized authenticated response). Rewrote the Design region and `Reason` string to
  state that accurately and to name the real hazard (requiring auth would break the anonymous demo
  path).
- Everything else matches the plan; no design issues required stopping to report.

### Follow-ups (recorded, out of scope here)

- No admin/role-based authorization policy exists yet — any authenticated principal can currently
  mutate demo roles under `identity-session-authenticated`. Real admin/role authz is future work.
- `get-sign-in-token`'s arbitrary-UserId-minting hazard (legacy Passwordless path) is slated for
  retirement alongside 104-016/104-021.
- `get-profile`'s real persistence + auth story is 104-016/104-024.
- 104-030 (api-server bearer wiring) shares this generator's auth-emission path and should build on
  the now-fail-closed default rather than reintroducing an anonymous-by-omission gap.

### Build / tests

- `dev build` (full solution): 0 Warning(s), 0 Error(s)
- `timewarp-architecture-analyzers-tests`: 82 passed (was 75; +7 new TWA0013/0014 tests)
- `timewarp-architecture-sourcegenerator-tests`: 40 passed (was 32; 2 flipped assertions + 2 new
  tests)
- `web-contracts-tests`: 26 passed (unaffected — no contract shapes changed, only attributes)
- `timewarp-identity-tests`: 168 passed (unaffected)
- `web-server-integration-tests`: 56 passed, 1 skipped, 0 failed (was 53 passed/1 skipped; +3 new
  `Roles_Authorization_Tests`, 2 `CreateRole_Endpoint_Tests` regressions fixed in place)
- Regression sweep — `foundation-domain-tests`: 37 passed; `foundation-contracts-tests`: 2 passed;
  `foundation-application-tests`: 13 passed; `foundation-infrastructure-tests`: 1 passed;
  `web-domain-tests`: 26 passed — all unaffected, 0 failures
- Docker-dependent suites: out of scope, not run

## Session

- Created: 2026-07-20
