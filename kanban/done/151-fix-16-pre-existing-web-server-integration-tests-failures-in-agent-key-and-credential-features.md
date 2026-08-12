# Fix 16 pre-existing web-server-integration-tests failures in agent-key and credential features

## Description

Discovered during task 150 (2026-08-05): `tests/container-apps/web/web-server-integration-tests`
has 16 failing tests at dev tip `c8ae9def` — before and independent of the task-150 diff
(verified by running the suite with all 150 changes stashed: identical 16 failures both ways).
Suite totals at `6bd81f13`: 112 total / 16 failed / 95 passed / 1 skipped.

All 16 are in agent-key / credential-management features. Names observed include
`ValidationError_Given_Empty_Name`, `Conflict_Given_Duplicate_Key`,
`Forbidden_Given_Quarantined_Principal` (full list falls out of the repro command below).

Unknown when they started failing — possibly since a recent auth/identity change (e.g.
`55ee9384` or the task 148/149 work) or an earlier regression that CI did not gate (this suite
runs via `dev test`, not the solution build). Root-causing when/why they broke is part of the
task.

## Requirements

- Bisect or otherwise identify the commit/change that introduced the failures.
- Fix product code or tests, whichever is actually wrong — do not blanket-skip.
- Full suite green: `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release`
  ends 0 failed (the 1 pre-existing skip may remain if intentional).
- Note whether CI should have caught this and, if so, what gate is missing (follow-up task if
  non-trivial).

## Checklist

- [x] Reproduce: run the suite at dev tip; capture the full list of 16 failing test names and
      their failure messages
- [x] Identify the introducing commit (git bisect over the suite or targeted class filters)
- [x] Root-cause each failure cluster (likely one shared cause across agent-key/credential
      features)
- [x] Fix; suite fully green
- [x] Assess CI gate coverage for this suite; file follow-up if a gap exists
- [x] Results with How to validate

## Notes

- Discovered/documented in task 150 Results ("Known issue out of scope"); the reviewer there
  confirmed the failures are confined to non-profile features.
- Fixed-port suite (web=7000, api=7255) — run serialized, no parallel test runs.

## Session

- Created: Claude (2026-08-05, during task 150 orchestration)
- 2026-08-12 (Grok): Reproduced 16 failures (117 total / 16 failed / 100 passed / 1 skip).
  Two independent clusters:
  1. **Abuse rate limit (task 104-015, commit `885be385`)** — 14 failures across
     agent-registration / agent-token / credential-add|list|revoke. Principal-registration
     window is ~10/min/IP; agent-registration alone burns >10 start+complete hits per class →
     mid-suite 429 / OneOf T2 ("Cannot return as T0 as result is T2") / no Set-Cookie.
  2. **CreateRole admin policy (task 147-004, commit `a0007945`)** — 2 failures
     (`ValidationError_Given_Empty_Name/UserId`). Endpoint is `CanViewRolesPage` (Administrator);
     tests only minted Member passkey sessions → 403 instead of 400. Design region still
     claimed task 110's identity-session-only posture.
  Fixes:
  - `WebTestServerApplication` PostConfigure `AbuseRateLimitOptions.Enabled = false` (abuse
    suite re-enables + tightens via configureWeb).
  - `create-role-endpoint-tests` grants Administrator via `IPrincipalRoleStore` after mint.
  Verified: suite 116 passed / 0 failed / 1 skip; abuse-rate-limiting-tests.cs 3/3; foundation
  infrastructure-tests 11/11 (pre-existing private-set fix already on branch).

## Results

### Root cause

Two unrelated regressions that CI only surfaced once this suite was gated:

| Cluster | Introduced by | Symptom | Fix |
|---------|---------------|---------|-----|
| Rate limit | `885be385` (104-015) | 429 / T2 on registration ceremonies after ~10 hits/class | Disable abuse limiter in integration host; abuse suite still proves 429 with tight PostConfigure |
| CreateRole 403 | `a0007945` (147-004) | Forbidden vs BadRequest on validation tests | Grant Administrator after passkey mint (same pattern as roles-authorization-tests) |

### Files changed

- `tests/common/timewarp-testing/applications/web-test-server-application.cs`
- `tests/container-apps/web/web-server-integration-tests/features/admin/roles/create-role/create-role-endpoint-tests.cs`
- `source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs` (Design + comment only)

### CI gate note

This suite already runs via `dev test` / the failed CI job — no missing gate; the failures
were pre-existing on dev and tracked here rather than fixed in 150. No follow-up needed for
coverage.

### How to validate

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release
# expect: 0 failed, 1 skip (RunForever)

dotnet run source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs -c Release
# expect: 3 passed (429 proof still live)
```
