# Fix yarp-off generated app aspire-tests that assume the ingress resource

## Description

A generated app made with `dotnet new timewarp-architecture ... --yarp false` builds clean
(`SmokeNoYarp` is in the template-smoke matrix since task 373), but its `aspire-tests` project
fails at runtime: 13 of 57 tests fail.

Found by task 373 while checking the single-flag-off variants. `dev template-smoke` builds the
solution and runs the Jaribu family aggregators, but it does not run `aspire-tests`, so this
runtime failure is not caught.

Observed failures (`dotnet test -c Release` in the generated `aspire-tests` project):
- Every fact in `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`: `SetupOnce`
  waits for the `ingress` resource, which does not exist without yarp ("Stopped waiting for
  resource 'ingress' to become healthy because it does not exist in the application model").
  This includes `StateDevTools_Should_RunAndServeItsUi`, which is not ingress-specific.
- `closed-box-app-host-guard-tests.cs`: `Ingress_Endpoints_Should_Not_Use_DevRun_Ports` and
  `Default_Config_Should_Keep_DevRun_Pins` call `Single(...)` for the ingress resource and throw
  "Sequence contains no matching element".

`--grpc false` aspire-tests pass (57/57). `--web false` is fixed by task 373, which excludes
`ingress-smoke-tests.cs` under `!web`.

## Goal (implementer restatement)

A generated `--yarp false` app has no `ingress` resource, so the aspire-tests that wait on or
read that resource must not compile into it. Guard every ingress-only gate and fact with
`#if yarp` (ingress smoke class and the closed-box guard's ingress model facts). Keep the facts
that do not need the ingress (web-server first-run log check, state dev tools UI) running when
yarp is off and web is on. The default app's aspire-tests must stay the same and green. Decide
whether `dev template-smoke` should run each variant's aspire-tests and record the decision with
its cost.

Acceptance: `--yarp false` aspire-tests green; default aspire-tests green and unchanged in
coverage; decision recorded; `dev build` 0/0; `dev test` and `dev template-smoke` green;
`ganda repo audit` clean.

## Requirements

1. Make `--yarp false` aspire-tests pass. Exclude or guard the ingress-only facts under `!yarp`.
   Facts that do not need the ingress (state dev tools, the first-run web-server log check)
   should still run when yarp is off and web is on. Move them out of the ingress file or point
   them at web-server directly.
2. Guard the ingress assertions in `closed-box-app-host-guard-tests.cs` with `#if yarp`.
3. Decide whether `dev template-smoke` should also run each generated app's `aspire-tests` so this
   class of runtime bug fails smoke. Record the decision and its cost.

## Checklist

- [x] Generated `--yarp false` app: aspire-tests green
- [x] Generated default app: aspire-tests unchanged and green
- [x] Decide and record whether template-smoke runs aspire-tests
- [x] `dev build` 0/0, `dev test` green, `dev template-smoke` green
- [x] `ganda repo audit` clean

## Notes

Reproduce: run `dev template-smoke`, then `dotnet test -c Release` in
`artifacts/template-smoke/work/SmokeNoYarp/tests/container-apps/aspire/aspire-tests`.

## Results

Generated aspire-tests now pass in every template-smoke variant. Running each variant's suite
turned up the same bug for two more flags, so the fix covers `api` and `postgres` as well as
`yarp`.

| Variant | Before | After |
|---------|--------|-------|
| SmokeDefault | 57/57 | 57/57 |
| SmokeNoYarp | 44/57 (13 failed) | 46/46 |
| SmokeNoApi | 46/57 (11 failed) | 56/56 |
| SmokeNoPostgres | 43/54 (11 failed, SetupOnce timed out after 4 min) | 54/54 |
| SmokeNoGrpc | 57/57 | 57/57 |
| SmokeNoWeb | 42/42 | 42/42 |

Changes (all in `tests/container-apps/aspire/aspire-tests/`):
- `ingress-smoke-tests.cs`: the `ingress` health gate, the ingress reachability poll, and every
  through-ingress fact compile only under `#if yarp`. `FirstRunOnEmptyDatabase_...` and
  `StateDevTools_...` stay outside the guard, so they still run with yarp off and web on (moved
  `StateDevTools_...` above the guarded block). The `api-server` gate and
  `ApiRouteThroughIngress_...` are under `#if api`. The `web-migrations` terminal-state gate is
  under `#if postgres`; without postgres there is no such resource and `WaitForResourceAsync`
  waits until the 4-minute budget cancels.
- `closed-box-app-host-guard-tests.cs`: the two ingress model facts and `DevRunIngressPorts` are
  under `#if yarp` (the field also has to be guarded, or the yarp-off build fails CA1823).
- `aspire-tests.csproj`: `DefineConstants` adds `api;postgres;yarp` (TWA0010).
- Guards put the blank line inside the `#if`, so no flag combination leaves a double blank line
  (IDE2000 broke the first SmokeNoYarp build).

### Decision: template-smoke does not run aspire-tests (yet)

Measured cost, run in sequence on this machine against the generated apps: Default 60s, NoYarp
57s, NoApi 59s, NoGrpc 59s, NoPostgres 24s, NoWeb 10s, so about 4.5 minutes extra for the full
matrix. Every suite except NoWeb also needs Docker (Postgres container plus DCP). Today
`dev template-smoke` needs only the SDK.

Decision: do not add aspire-tests to `dev template-smoke` in this task. The CI policy is "default
variant per PR, flag-off matrix nightly", and the bugs found here only show up in flag-off
variants, so the default-only per-PR run would not have caught them. These suites belong in the
nightly flag-off matrix job, not in the per-PR smoke. Open for Steve: wire that job (or an opt-in
`dev template-smoke --aspire-tests` switch for local runs) as a follow-up task.

### How to validate

Smoke:
```bash
dev build && dev template-smoke
for v in SmokeDefault SmokeNoYarp SmokeNoApi SmokeNoGrpc SmokeNoPostgres SmokeNoWeb; do
  (cd artifacts/template-smoke/work/$v/tests/container-apps/aspire/aspire-tests && dotnet test -c Release)
done
dev test && ganda repo audit
```

Expect: `dev build` 0 warnings / 0 errors; template-smoke prints `OK` for all six variants;
each aspire-tests run ends `Test run summary: Passed!` (Default 57, NoYarp 46, NoApi 56, NoGrpc
57, NoPostgres 54, NoWeb 42); `dev test` reports 23 of 23 projects passed; audit reports
"Repository passes all audit checks." Docker must be running for the aspire-tests runs.

### Review disposition

- Rounds: 1. Effort 1, roster `general` (review oracle, claude-opus-5-5).
- Final counts: bug 0, suggestion 0, nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions**. M1 (nit): the Design and SetupOnce comments describe the
  all-flags gate sequence. It is not fixed because the Task 374 Design paragraph already documents
  which gates each flag guards.
- Reviewer checked the generated SmokeNoYarp, NoApi, and NoPostgres output and re-ran
  `ganda repo audit` (pass).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

## Learnings

- The goal restatement matches the task, so there was no misread. The api/postgres guards were
  scope found by running every variant's aspire-tests, and Results records them.

## Session

- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T05:56:12Z
- Review oracle: claude-opus-5-5, effort 1, round 1, accepted-exceptions — 2026-10-16
