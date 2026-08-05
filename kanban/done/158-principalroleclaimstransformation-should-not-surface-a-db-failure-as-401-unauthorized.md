# PrincipalRoleClaimsTransformation should not surface a DB failure as 401 Unauthorized

## Description

Found while working task 155 (AppHost web-server restart deadlock / hybrid on-demand
migrations). `aspire-tests`' `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
(`tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`) returns `401 Unauthorized`
instead of the expected `403 Forbidden` for a mock principal that is authenticated but not an
admin.

**Important correction (2026-08-05):** this was initially diagnosed as a migration-timing race
introduced by task 155 (web-server serving before `RunDatabaseUpdateOnStart` finishes). That
diagnosis is **disproven**: the same failure reproduces identically (3/3 runs) on the
**unmodified baseline** — original `WaitFor(webMigrations)`, none of task 155's changes applied
— where migrations are long finished by the time any test request fires. This is a **pre-existing
bug on `dev`, unrelated to task 155's wait-edge changes**, not something task 155 introduced or
is blocked by. Root cause is genuinely unknown — do not assume it's DB/migration-timing related
without re-investigating from scratch.

What's still true and worth fixing regardless of what turns out to cause *this specific* test
failure: the authorization code path is structurally capable of mislabeling a DB failure as an
authentication failure. Mock authentication itself is pure in-memory
(`source/container-apps/web/platform/identity-host/mock-identity-principal-handler-server.cs`) —
no DB access. Role resolution for authorization runs through `IClaimsTransformation` →
`PrincipalRoleClaimsTransformation`
(`source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs`)
→ `EffectiveRolesResolver.GetEffectiveRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs`)
→ `EfPrincipalRoleStore.GetRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs`),
which queries Postgres directly. If that EF query throws for any reason (missing schema,
connection pool exhaustion, network blip, Postgres failover), whether or not that's what's
actually happening in the failing test today, a DB failure there should not present as "not
authenticated" — it should fail closed as 403 or surface as a genuine 5xx. That's a real,
independent hardening gap worth fixing on its own merits.

## Requirements

- **First: find the actual root cause** of `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  returning 401 instead of 403 through the closed-box `aspire-tests` ingress path. Candidates to
  investigate fresh (none confirmed): mock-auth header not honored under the `Production`
  environment the closed-box AppHost boots as (confirmed via earlier task-155 log:
  `EnvironmentName: Production`); YARP not forwarding the `X-TimeWarp-Mock-Principal-Id` header
  end-to-end; a genuine bug in `EfPrincipalRoleStore`/`PrincipalRoleClaimsTransformation`
  unrelated to timing; something else entirely. Do not reuse the migration-race explanation —
  it's ruled out.
- **Separately:** a DB failure inside `PrincipalRoleClaimsTransformation` / `EffectiveRolesResolver`
  / `EfPrincipalRoleStore` must not present as 401 Unauthorized for an otherwise-successfully
  -authenticated principal. Decide and implement the correct fail-closed behavior (403-as-no-roles
  vs. propagate-as-5xx) — agree with the maintainer before implementing, don't assume.
- Add/extend a test proving the corrected status code under a simulated role-store failure
  (e.g. inject a failing `IPrincipalRoleStore` fake in an in-proc suite — do not depend on
  racing real Postgres migrations to reproduce this).

## Checklist

- [x] Reproduce `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` in
      isolation and find the actual root cause (see candidates above) — pinned by Grok
      root-cause investigation (missing FE `AuthSchemes(...)` emission), confirmed by the fix
      below turning the test green
- [x] Confirm with maintainer which fail-closed behavior is wanted for the general DB-failure
      mislabeling — maintainer decision 2026-08-05: **spun out to task 160** (dedicated
      hardening task; decision + implementation live there)
- [x] Implement the fix(es) — root cause of the failing test **only** (FE `AuthSchemes` emission
      for the closed-box mock scheme gap). The general `PrincipalRoleClaimsTransformation` /
      `EffectiveRolesResolver` / `EfPrincipalRoleStore` DB-failure hardening is **not**
      implemented — separate maintainer decision pending, per explicit instruction
- [x] Add a deterministic test (DI-substituted failing store, not a live-DB race) for the
      general hardening — **moved to task 160** with the rest of the hardening scope
- [x] Reconcile any `#region Design` blocks touched
- [x] Results with How to validate

## Notes

- Related: task 155
  (`kanban/in-progress/155-apphost-web-server-restart-deadlocks-on-finished-web-migrations-waitfor.md` /
  eventual `done/`) — hybrid on-demand migrations; this failing test was found there, but task
  155 does not cause it and is not blocked by it. Task 155's `ingress-smoke-tests.cs` SetupOnce
  does now wait for `web-migrations` to reach a terminal state before firing requests (defensive,
  closes the *actual* migration-race window for this suite) — that wait is harmless and worth
  keeping, but it did not fix this test, confirming the cause lies elsewhere.
- This is scoped independently of 155's AppHost/orchestration changes — no AppHost or migration
  wiring should need to change for this fix.

### Root-cause investigation (Grok) — 2026-08-05

**Pinned root cause:** mock-auth never authenticates on the closed-box GetRoles path because the **`mock-identity-session` scheme is never invoked** for that endpoint. The request is treated as anonymous and the cookie scheme challenges with **401**. This is **not** YARP dropping the header, **not** Production env gating mock off, and **not** `PrincipalRoleClaimsTransformation`/DB mislabeling for this specific failure.

**Classification:** `mock-auth-never-authenticates` (scheme never runs) — not `header-dropped-at-YARP`, not `authenticated-but-401-from-role-path`.

#### Evidence chain (falsifiable)

1. **Repro confirmed:** `cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-method Forbidden_Given_MockPrincipal` → Expected `403 Forbidden`, Actual `401 Unauthorized` (deterministic).
2. **Env + UseMock are correct on the live web-server process** (rules out task-155 Production theory for this suite):
   - AppHost config: `Authentication:UseMock = 'true'` (from test args).
   - `/proc/<pid>/environ` for the DCP-launched web-server: `ASPNETCORE_ENVIRONMENT=Development`, `Authentication__UseMock=true`.
   - Serilog boot line: `EnvironmentName: Development`.
3. **YARP ruled out:** same 401 when calling **web-server directly** (`CreateHttpClient("web-server", "http"|"https")`) with the mock header — ingress is not required for the failure.
4. **Session probe note:** `/api/identity/session` with mock header still shows `isAuthenticated:false` — expected for AllowAnonymous (only default scheme runs); not proof about Roles.
5. **Handler never reached (decisive):** temporary instrumentation that throws `InvalidOperationException("MOCK_AUTH_HANDLER_REACHED_…")` at the top of `MockIdentityPrincipalHandler.HandleAuthenticateAsync` when path contains `Roles`:
   - **Without** endpoint `AuthSchemes(...)`: anonymous Roles smoke still returns **401** and **passes** — throw never executes → **mock scheme not in the effective auth scheme list**.
   - **With** temporary `[EndpointAuthorize(..., AuthenticationSchemes = "identity-session,mock-identity-session")]` so generator emits `AuthSchemes("identity-session", "mock-identity-session")`: anonymous Roles smoke **fails** (no longer clean 401) — mock handler **is** entered.
6. **In-proc control:** `roles-authorization-tests` Member-only passkey cookie → **403** on GetRoles — proves `CanViewRolesPage` Forbid path works when a scheme actually authenticates. Closed-box only lacks a scheme that accepts the mock header under current FE emission.
7. **Code shape today:**
   - Generated `GetRolesEndpoint`: only `Policies("CanViewRolesPage");` — **no** `AuthSchemes(...)`.
   - Server `AddPolicy(CanViewRolesPage)` does list `identity-session` + `mock-identity-session`, but FE also attaches `epPolicy:<EndpointType>` with bare `RequireAuthenticatedUser()` and no schemes; **without** FE-level `AuthSchemes`, mock is not exercised (empirically).
   - Generator already supports schemes when `[EndpointAuthorize(AuthenticationSchemes=…)]` is set (`BuildAuthConfiguration`).

#### Did this test ever pass?

| Version | Expectation | Ever proven green? |
|---------|-------------|-------------------|
| `e084c1ba` (145-009) | `RolesThroughIngress_Should_Ok_Given_MockPrincipalHeader` → **200** | Task 145-009 Results claim aspire-tests **7/7** including that test. Same missing-`AuthSchemes` emission pattern existed then (`Policies("identity-session-authenticated")` only). That claim is **not independently re-verified here**; given current scheme evidence, treat as **untrusted / possibly never truly authenticated via mock**. Neither 145-009 nor 147-004 is on `master` yet. |
| `a0007945` (147-004) | renamed to `…Forbidden…` → **403** | **No evidence of green on CI/dev.** Pre-existing failure on `dev`; not introduced by task 155. |

Git archaeology highlights:
- Mock scheme + OK test: `e084c1ba` (145-009).
- Policy → Administrator + expect 403: `a0007945` (147-004).
- AppHost UseMock opt-in only (test already passes `--Authentication:UseMock=true`): `55ee9384`.
- Fail-closed real-env gate (R2-1): `2eb5416d` — **not** the closed-box failure mode here (env is Development).

#### Proposed minimal fix (describe only — not implemented)

1. **Make closed-box mock scheme actually run for admin API endpoints** by emitting FE `AuthSchemes` for the same schemes the server policies already declare:
   - Preferred: set `[EndpointAuthorize(Policy = …, AuthenticationSchemes = "identity-session,mock-identity-session")]` on admin contracts that list mock on the server policy (GetRoles + siblings), **or**
   - Better convention: generator/default for policies that include mock (or all identity-session BFF policies) always emit `AuthSchemes("identity-session", "mock-identity-session")` so FE `AuthorizeAttribute.AuthenticationSchemes` + `epPolicy` path cannot silently drop to default-scheme-only.
2. **Re-prove:** aspire-tests Forbidden mock test → **403**; anonymous Roles → **401**; optional direct web-server header probe.
3. **Do not** “fix” this 401 by relaxing role policy or by turning Production-safe mock on.

#### Separate hardening decision input (DB failure → not 401)

Independent of the closed-box mock bug: `IClaimsTransformation` → `PrincipalRoleClaimsTransformation` → `EfPrincipalRoleStore` can still throw after a **successful** auth. Prefer **propagate as 5xx** (failed authZ infrastructure) over **403-as-no-roles** (lies about authorization outcome; can mask outages). Implement only after maintainer call; cover with in-proc DI-failing `IPrincipalRoleStore`, not migration races.

#### What was / was not modified this session

- Investigation only: temporary local instrumentation was applied and **fully reverted**.
- **Only intended durable edit:** this Notes/Session append on task 158.
- No production code commits; working tree left without investigation residue on product files.

## Session

- Created: Claude (2026-08-05), spun out of task 155 architecture discussion with maintainer
  (Steve) — agreed as follow-up scope, not blocking 155's close.
- 2026-08-05: corrected root-cause claim after baseline verification (stash + rerun on
  unmodified `dev`) showed the failure predates and is unrelated to task 155's changes; migration
  -race theory disproven, root cause reopened as unknown.
- Root-cause investigation (Grok Build): session `5f915c56-81a8-4972-b943-15fd3c83aa97` (2026-08-05) — investigation only; findings under Notes.
- Implementation: Claude (2026-08-05) — closed-box mock-scheme fix per maintainer-approved shape
  (Notes "Proposed minimal fix" §1). `EndpointAuthorizeAttribute.AuthenticationSchemes` and the
  generator's `AuthSchemes(...)` emission already existed on `dev` (task 110, commit `44fd802f`)
  — unused by any contract. Added `AuthenticationSchemeNames` contracts-visible constants
  (`source/container-apps/web/features/identity/authentication-scheme-names-contracts.cs`,
  Features substrate, mirrors `AuthorizationPolicyNames`) and applied
  `AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," +
  AuthenticationSchemeNames.MockIdentitySession` to the 7 contracts gated by
  `CanViewRolesPage`/`CanViewPrincipalsPage` (the two policies whose server `AddPolicy(...)` call
  already lists `mock-identity-session`). Did **not** touch `credential-management` (dual
  identity-session/agent-token policy, no mock, no proven failure) or the two agent-token-only
  policies (`agent-scope:identity:read`, `agent-scope:demo:invoke`) — scoped narrowly to the
  proven failure mode; flagged for maintainer follow-up if broader consistency is wanted.
  Added a generator test (`Should_Emit_Both_AuthSchemes_And_Policies_When_Both_Set`) proving the
  combined `Policy` + multi-scheme `AuthenticationSchemes` emission shape now used in contracts.
  Gates: `./bin/dev build --clean` 0/0; `timewarp-architecture-sourcegenerator-tests` 22/22;
  `timewarp-architecture-analyzers-tests` (TWA0013/0014) 9/9 unaffected;
  `aspire-tests` 7/7 (`RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  now 403; `RolesThroughIngress_Should_ReachWebServerAndRequireAuth` still 401);
  `web-spa-integration-tests` 15/15 + 1 skip (no regression); `api-server-integration-tests` 1/1;
  in-proc `roles-authorization-tests` (`RolesAuthorization`) 6/6 (no regression). Did not touch
  the fail-closed 5xx DB-failure hardening scope — left for a separate maintainer decision.

## Results

### Summary

`RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` now returns **403**
(was 401) through the closed-box `aspire-tests` ingress path. Root cause (pinned by Grok's
investigation, confirmed by this fix): the generated FastEndpoint for admin BFF contracts
(`GetRoles` and siblings) emitted only `Policies("CanViewRolesPage")`, never `AuthSchemes(...)`,
so the `mock-identity-session` authentication handler was never invoked for that route — the
request stayed anonymous and the default cookie scheme challenged with 401 instead of the
policy's role check running and denying with 403.

Fix: the FastEndpoint generator already supported emitting `AuthSchemes(...)` from
`[EndpointAuthorize(AuthenticationSchemes = "...")]` (task 110, present on `dev` before this
session) — it was simply never used by any contract. This session added a contracts-visible
`AuthenticationSchemeNames` constants class and applied `AuthenticationSchemes` to the 7 contracts
gated by `CanViewRolesPage`/`CanViewPrincipalsPage`, mirroring exactly the scheme list already
declared on those policies' server-side `AddAuthenticationSchemes(...)` call.

### Production-safety mechanism (mock scheme is safe to list unconditionally)

`mock-identity-session` is **always registered** — `MockIdentityPrincipalHandler` is added via an
unconditional `.AddScheme<AuthenticationSchemeOptions, MockIdentityPrincipalHandler>(...)` call in
`source/container-apps/web/projects/web-server/program.cs:396` (inside `ConfigureAuthentication`,
which always runs). Listing the scheme name in `AuthSchemes(...)` therefore never throws
`InvalidOperationException` for an unregistered scheme, in any environment.

What makes Production safe is not conditional registration — it's a **fail-closed handler**:
`MockIdentityPrincipalHandler.HandleAuthenticateAsync`
(`source/container-apps/web/platform/identity-host/mock-identity-principal-handler-server.cs:44-50`)
returns `AuthenticateResult.NoResult()` unless
`MockAuthenticationDefaults.IsMockAuthActive(environment.EnvironmentName, configuration[...UseMockKey])`
is true — i.e. unless the host is booted `Development`/`Testing` **and** `Authentication:UseMock`
is set. A Production-booted host (or any host without the config flag) gets `NoResult()` from the
handler every time, regardless of which endpoints list the scheme in `AuthSchemes(...)`. This is
the same mechanism task 145-009 already relies on for `IdentitySessionDefaults.AuthenticatedPolicy`
(`program.cs:184`, `AddAuthenticationSchemes(IdentitySessionDefaults.Scheme,
MockIdentityPrincipalHandler.SchemeName)`) — this fix simply makes the FE-generated endpoint
metadata consistent with policies that already listed the scheme.

### What changed

| Path | Change |
|------|--------|
| `source/container-apps/web/features/identity/authentication-scheme-names-contracts.cs` | New — `AuthenticationSchemeNames` (IdentitySession / MockIdentitySession / AgentToken) contracts-visible constants, Features substrate |
| `source/container-apps/web/features/admin/roles/{create,get-role,get-roles,update-role,delete-role}/*-contracts.cs` | Added `AuthenticationSchemes = IdentitySession + "," + MockIdentitySession` to `[EndpointAuthorize(Policy = CanViewRolesPage)]`; reconciled Design regions |
| `source/container-apps/web/features/admin/principals/{list-principals,set-principal-roles}/*-contracts.cs` | Same, for `CanViewPrincipalsPage` |
| `tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-tests.cs` | New test `Should_Emit_Both_AuthSchemes_And_Policies_When_Both_Set` proving combined `Policy` + multi-scheme `AuthenticationSchemes` emission |

No changes to `EndpointAuthorizeAttribute`, the generator, or TWA0013/0014 — all pre-existed and
needed no modification (verified via their own passing test suites).

### Out of scope (deliberately untouched)

- `credential-management` policy contracts (`add-agent-key`, `add-passkey`, `get-credentials`,
  `revoke-credential`) — dual identity-session/agent-token policy, does **not** declare
  `mock-identity-session`, no proven failure. Left alone to keep this fix's blast radius to the
  proven bug; flagged here in case the maintainer wants the same `AuthSchemes` consistency applied
  there too (`identity-session,agent-token`, mirroring `CredentialManagementDefaults.Policy`'s own
  scheme list).
- `agent-scope:identity:read` / `agent-scope:demo:invoke` (agent-token-only policies) — explicitly
  excluded per instruction; `AgentTokenDefaults`'s own Design region documents deliberate scheme
  isolation.
- The general `PrincipalRoleClaimsTransformation` DB-failure-as-401 hardening (this task's
  original title) — separate maintainer decision pending, not implemented this session.

### How to validate

```bash
./bin/dev build --clean
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release -- --filter-method Should_Emit_Both_AuthSchemes_And_Policies_When_Both_Set
cd ../../../tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class EndpointAuthPosture
cd ../../container-apps/aspire/aspire-tests && dotnet test -c Release
cd ../../web/web-spa-integration-tests && dotnet test -c Release
cd ../../api/api-server-integration-tests && dotnet test -c Release
cd ../../web/web-server-integration-tests && dotnet test -c Release -- --filter-class RolesAuthorization
```

**Expect:** build 0/0; generator test passes; TWA0013/0014 tests 9/9; `aspire-tests` 7/7 (mock
non-admin → 403, anonymous → 401); `web-spa-integration-tests` 15/15 + 1 skip; `api-server-integration-tests`
1/1; `roles-authorization-tests` 6/6.
- 2026-08-05 claude (orchestrator): maintainer split remaining scope — task 160 (fail-closed
  DB-failure hardening, decision + implementation) and task 161 (research: should
  credential-management contracts declare AuthenticationSchemes). This task closes on the fixed
  and fully-green 401-vs-403 bug (commit 6442b605, aspire-tests 7/7).
