# Role resolution DB failure must not surface as 401 (fail-closed hardening)

## Description

Spun out of task 158 (its general-hardening half; the 401-vs-403 test failure itself was fixed
there — missing `AuthSchemes` on generated endpoints, commit `6442b605`).

The authorization role-resolution path is structurally capable of mislabeling a DB failure as an
authentication failure: `IClaimsTransformation` → `PrincipalRoleClaimsTransformation`
(`source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs`)
→ `EffectiveRolesResolver.GetEffectiveRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs`)
→ `EfPrincipalRoleStore.GetRoleIdsAsync`
(`source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs`),
which queries Postgres. If that EF query throws (connection pool exhaustion, network blip,
Postgres failover, missing schema), an otherwise-successfully-authenticated principal must NOT be
presented as "not authenticated" (401). It should fail closed with an honest status.

## Requirements

- **Decide the fail-closed behavior with the maintainer before implementing** —
  503/5xx (surface the infrastructure failure honestly) vs 403-as-no-roles (treat unresolvable
  roles as no grants). Grok's investigation input (task 158 Notes): prefer **5xx** — a DB outage
  is not an authorization verdict, and 403-as-no-roles hides real infrastructure failures from
  operators while confusing users with a permissions message.
- Implement so a role-store failure for an authenticated principal produces the chosen status,
  never 401.
- Deterministic test: DI-substitute a failing `IPrincipalRoleStore` fake in an in-proc suite
  (do NOT depend on racing real Postgres to reproduce).
- Reconcile `#region Design` blocks in touched files.

## Checklist

- [x] Confirm fail-closed behavior with maintainer (5xx vs 403-as-no-roles; Grok input: 5xx)
- [x] Implement in the claims-transformation / resolver path
- [x] Deterministic in-proc test with failing IPrincipalRoleStore fake (authenticated principal
      → chosen status, not 401)
- [x] Reconcile Design regions
- [x] Results with How to validate
- [x] Implementation review (effort 1, general) — disposition clean (round 1 + round 2)
- [x] CI blocker: bump SmokeDefault `web-jaribu-tests` expected count 102 → 104; PR #321 green

## Notes

- **CI BLOCKER resolved (implementer 2026-09-04):** PR #321 `template-smoke` was red because
  SmokeDefault `web-jaribu-tests` reported **104/104** vs harness expected **102**. Bump is
  commit `b4d82514` (`tools/dev-cli/services/template-smoke-harness.cs` line 578 → 104).
  Did **not** re-litigate the 503 design. Restored uncommitted `.gitignore` dirt
  (`*.journal.json` / `.memsearch/memory/` belong on task 208, not this branch). Dropped
  unpushed local merge of `origin/feature/overnight` (workflow trigger edits) so this PR
  stays on master-only CI. Green: Actions run `33864521962` on SHA `01925db0` —
  `detect-paths`, `ci`, `template-smoke` SUCCESS.
- Origin: task 158 (root-cause investigation and full evidence chain live there; see its
  "Root-cause investigation (Grok)" Notes section and Results).
- Behavior context: today an exception inside `IClaimsTransformation` propagates through the
  authentication middleware — verify exactly where it turns into 401 vs 500 as part of
  implementation (Grok classified the mechanism; re-verify at implement time).
- Fail-closed decision (implementer 2026-09-04): **503 Service Unavailable**, not 403-as-no-roles.
  This is the 5xx choice recorded on this task from Grok's task-158 investigation input and the
  maintainer's spin-out of that hardening half. A role-store outage is infrastructure, not an
  authorization verdict; empty-roles 403 would hide the outage and lie to the user.
- Mechanism re-verified at implement time against ASP.NET Core 10.0.11:
  - `AuthenticationService.AuthenticateAsync` calls `IClaimsTransformation.TransformAsync` only
    after the handler `AuthenticateResult.Succeeded`; there is **no try/catch**. A throw leaves
    `AuthenticateAsync`.
  - `AuthenticationMiddleware` assigns `context.User` only from that result; a throw never becomes
    `AuthenticateResult.Fail`.
  - `PolicyEvaluator.AuthorizeAsync`: `authenticationResult.Succeeded ? Forbid : Challenge`.
    Challenge is cookie `OnRedirectToLogin` → **401** for `/api`. That is the 401 mislabel path if
    a store failure is converted to a failed/anonymous authenticate (or swallowed as no identity).
    Swallowing as no role claims would instead Forbid → **403**.
  - Development `UseDeveloperExceptionPage` would map an *unmapped* throw to **500**.
    `RoleResolutionFailureMiddleware` is registered *after* the Dev page (inner) and *before*
    `UseAuthentication`, so it writes 503 and does not rethrow; the Dev page sees a completed
    response. Production has no Dev page; the middleware is the mapper.

## Session

- Created: Claude (2026-08-05), spun out of task 158 per maintainer direction.
- Implementer: Grok (2026-09-04) — 503 fail-closed hardening + in-proc DI fake test.
- Review: Grok (2026-09-04) — effort 1 general, round 1, disposition clean.
- Implementer (re-verify): Grok (2026-09-04) — remaining product work none; re-ran
  host-free 10/10, RoleResolutionFailure 2/2, RolesAuthorization 7/7.
- Review (round 2): Grok (2026-09-04) — effort 1 general, independent re-review after
  implementer re-verify; 0 issues; disposition remains clean.
- Implementer (CI blocker): Grok (2026-09-04) — confirmed 104 bump on origin, restored
  `.gitignore`, dropped unpushed overnight merge, host-free 10/10, PR #321 CI green.

## Results

### Summary

Role-store read failures for an otherwise-authenticated principal now return **503 Service
Unavailable**, never 401 (not authenticated) and never 403 (no grants). The 5xx choice is the one
recorded on this task (Grok task-158 input; DB outage is not an authorization verdict). 503 is
the specific 5xx: the service cannot resolve roles right now, as distinct from an unhandled 500
crash.

`EffectiveRolesResolver` wraps `IPrincipalRoleStore.GetRoleIdsAsync` failures as
`RoleResolutionFailedException` (does not wrap `OperationCanceledException` or an already-typed
`RoleResolutionFailedException`). `PrincipalRoleClaimsTransformation` does not catch that throw.
`RoleResolutionFailureMiddleware` maps it to 503. The middleware sits outside authentication,
authorization, and endpoints, so Transform, `PermissionEvaluator`, and handlers all get the same
status.

### Files changed

| Path | Change |
|------|--------|
| `source/container-apps/web/features/admin/principals/role-resolution-failed-exception-application.cs` | New typed store-read failure |
| `source/container-apps/web/features/admin/principals/role-resolution-failure-middleware-server.cs` | Maps that exception to HTTP 503 |
| `source/container-apps/web/features/admin/principals/effective-roles-resolver-application.cs` | Wrap store throws; Design region |
| `source/container-apps/web/features/admin/principals/i-effective-roles-resolver-application.cs` | Design + exception docs |
| `source/container-apps/web/features/admin/principals/principal-role-claims-transformation-server.cs` | Design: do not swallow |
| `source/container-apps/web/projects/web-server/program.cs` | Register middleware before `UseAuthentication` |
| `source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs` | Host-free wrap + cancellation tests |
| `tests/container-apps/web/web-server-integration-tests/features/admin/principals/role-resolution-failure-tests.cs` | In-proc DI-substituted failing store → 503, anonymous still 401 |
| `tools/dev-cli/services/template-smoke-harness.cs` | SmokeDefault `web-jaribu-tests` expected count 102 → 104 (the two wrap/cancellation tests) |

### Key decisions

- **503, not 403-as-no-roles, not generic 500.** 403 would treat an outage as "no permission."
  500 is what DeveloperExceptionPage already did for an unmapped throw; 503 names infrastructure.
- **Wrap at the resolver, not the EF store**, so any `IPrincipalRoleStore` (in-memory, EF, test
  fake) fails the same way.
- **Do not catch in `IClaimsTransformation`.** Returning the principal without roles is 403;
  returning an unauthenticated principal is 401.
- **Smoke expected count is a hand list.** Adding two host-free tests to the co-located
  web runfile grew the generated-app aggregator from 102 to 104; bump the harness in the
  same PR or `template-smoke` fails on counts alone.

### Test outcomes

- Host-free `effective-roles-resolver-tests.cs`: **10/10** (8 resolver including wrap +
  cancellation, 2 first-admin). Re-ran this session: 10/10.
- In-proc `RoleResolutionFailure`: **2/2** — anonymous GetRoles still 401; passkey cookie +
  throwing `IPrincipalRoleStore.GetRoleIdsAsync` → **503**, not 401/403. Re-ran this
  session: 2/2.
- Regression `RolesAuthorization`: **7/7**.
- PR #321 CI (SHA `01925db0`, Actions `33864521962`): `detect-paths`, `ci`,
  `template-smoke` **SUCCESS**. SmokeDefault `web-jaribu-tests` 104/104.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class RoleResolutionFailure
```

**Expect:** 2 passed. `Unauthorized_Given_Anonymous_Get_When_RoleStoreThrows` → HTTP 401.
`ServiceUnavailable_Given_AuthenticatedPrincipal_When_RoleStoreThrows` → HTTP 503 (not 401, not
403). The suite boots its own in-proc web host with a Get-throwing `IPrincipalRoleStore` fake;
no Postgres is required.

**Automated gate**

```bash
dotnet run source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs
# expect: 10 passed (includes StoreReadFailure_Should_ThrowRoleResolutionFailedException
#         and Cancellation_Should_NotWrapAsRoleResolutionFailed)

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class RoleResolutionFailure
# expect: 2 passed (anonymous 401, authenticated+failing store 503)

dotnet test -c Release -- --filter-class RolesAuthorization
# expect: 7 passed (no regression on Member 403 / Admin 200)
```

**CI / template-smoke**

```bash
gh pr checks 321
# expect: detect-paths, ci, template-smoke SUCCESS
# harness: tools/dev-cli/services/template-smoke-harness.cs JaribuFamilyAggregators
#          web-jaribu-tests ExpectedSucceeded == 104
```

**Expect:** PR #321 checks green. SmokeDefault generated-app `web-jaribu-tests` is 104 passed
of 104 expected. Do not restore expected count to 102.

**Depends on:** in-proc `HostGraphFactory` (fixed port web=7000). `dev test` serializes this
project for that reason. Mock/passkey ceremony helpers mint the cookie; the fake store's
`TryClaimFirstAdministratorAsync` is a no-op so registration can complete before Get throws.

**Not in scope:** racing a live Postgres outage; closed-box Aspire ingress (task 158 already
fixed mock-scheme 401 vs 403 there). Adding `*.journal.json` / `.memsearch/memory/` to this
branch `.gitignore` (task 208).

### Review disposition

- **Outcome:** clean
- **Rounds:** 2
- **Effort / roster:** 1 (general only)
- **Final counts:** bug 0 / suggestion 0 / nit 0 (all open/fixed/wontfix zero)
- **Wontfix / escalations:** none
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/round-2/general.md`
  - `review/round-2/merged.md`
  - `review/disposition.md`
