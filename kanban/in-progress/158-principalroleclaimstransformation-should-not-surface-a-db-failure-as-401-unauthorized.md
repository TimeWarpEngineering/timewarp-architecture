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

- [ ] Reproduce `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` in
      isolation and find the actual root cause (see candidates above) — this is currently unknown
- [ ] Confirm with maintainer which fail-closed behavior is wanted for the general DB-failure
      mislabeling (403-as-no-roles vs. propagate-as-5xx) before implementing
- [ ] Implement the fix(es) — root cause of the failing test, and/or the general hardening,
      depending on what the root-cause investigation finds
- [ ] Add a deterministic test (DI-substituted failing store, not a live-DB race) for the
      general hardening
- [ ] Reconcile any `#region Design` blocks touched
- [ ] Results with How to validate

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
