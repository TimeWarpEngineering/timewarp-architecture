# dev test runs AppHost-booting suites one at a time, while other projects stay parallel

## Problem (2026-10-15, master PR #515, `feature/ci-speed`)

Task 370 made `dev test` run projects in parallel: up to 4 at once, and at most 3 in the integration
group. On #515 the **push** CI run failed `ci (integration)` while the **PR** run on the same commit
passed. In the failing run:
- `yarp-integration-tests` failed every test in setup ("The response ended prematurely",
  `EndOfStreamException`).
- Earlier, the Aspire DCP logged `Could not perform substitution for environment variable
  ASPNETCORE_URLS … portForServing "web-server-https"`, the same DCP failure mode the AppHost Design
  region records (tasks 155 and 270).

It was running `api-jaribu-tests`, `api-server-integration-tests` and `aspire-tests` at once, plus
`web-infrastructure-tests`. **More than one closed-box suite boots a real Aspire AppHost**
(`DistributedApplicationTestingBuilder`, via the `ClosedBoxAppHost` helper from 319). Concurrent
AppHost/DCP startups interfere, so the result is flaky.

This is part of the CI speed-up stack: it runs as a child of `feature/ci-speed`, and #515 picks it up.

## Requirements

- **Exclusive lane for AppHost suites.** Projects that boot an Aspire AppHost never run at the same
  time as each other. Everything else stays parallel with them and with each other.
  - **Find AppHost suites by derivation, not a hand list.** For example, a project references
    `Aspire.Hosting.Testing` or compiles `tests/common/closed-box-app-host/closed-box-app-host.cs`.
  - Add a guard test that every AppHost-booting project is classified exclusive.
- Keep 370's per-project port-base slots and Docker concurrency limits. Testcontainers suites
  (web-infrastructure) may stay parallel with AppHost suites unless evidence says otherwise; record it.
- Investigate whether `yarp-integration-tests` failed because of the AppHost interference or
  something else, such as its own standalone YARP host and port. Make it deterministic either way.
- **Proof:** run the integration group **10×** in CI (a throwaway loop job, or `workflow_dispatch`
  reruns), or 10× locally with `-j 4` on a 4-core-equivalent limit. Zero failures, with timings. The
  integration job may get slightly slower; record before/after.

## Checklist

- [x] Derived AppHost-suite classification + guard test
- [x] Exclusive lane in the parallel runner; other projects stay parallel
- [x] yarp-integration root cause recorded and fixed
- [x] 10× integration runs green; timing before/after
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-15 (cockpit; #515 push-run failure)
- 2026-10-15 implement oracle (ganda task work): lane, port range, Postgres start hardening, 10× proof.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-15T14:21:41Z
- Review oracle (claude, claude-opus-5-5, effort 2, general): round 1, accepted-exceptions — 2026-10-15

## Results

### What changed

- **AppHost lane** (`tools/dev-cli/services/test-parallelism.cs`, `tools/dev-cli/endpoints/test-command.cs`).
  `TestParallelism.BootsAppHost` reads each csproj and follows its `ProjectReference`s. A project is an
  AppHost suite when it has an `Aspire.Hosting.Testing` `PackageReference` or a `Compile` of
  `closed-box-app-host.cs`. The dispatcher starts an AppHost suite only while no other AppHost suite
  is running. Everything else stays parallel, with 370's port slots and `--max-integration` cap
  unchanged. AppHost suites start first within the integration group so the serial chain begins at
  once. `dev test` prints the derived set. Today that set is api-server-integration-tests,
  aspire-tests and web-spa-integration-tests.
- **Guards** (`tests/tools/dev-cli-tests/test-parallelism-tests.cs`). One test pins the monorepo's
  AppHost set. Another scans every test project's sources (its own folder plus literal `Compile`
  includes) for a call to `DistributedApplicationTestingBuilder.CreateAsync` or
  `ClosedBoxAppHost.CreateAsync`, and asserts that each project it finds is classified exclusive. A
  third asserts that no co-located `*-tests.cs` under `source/` boots an AppHost. The
  `*-jaribu-tests` aggregators pull those files in with wildcard includes the scan does not follow.
  There are also fixture tests for direct, `Compile`, transitive and comment-only cases.
- **yarp-integration root cause.** The cause was a port collision, not DCP interference. In the
  failing push run (38728829679), yarp-integration-tests got port base **35000** and failed in
  `SetupOnce` with `Failed to bind to address https://[::1]:35000: address already in use`. With
  task 370, workers walk the derived ring from the checkout base (CI: 30500, 32000, 33500, 35000).
  The top slots sit inside the Linux ephemeral range (32768–60999). Task 330 accepted that overlap,
  but under parallel runs the OS handed that port to a socket from the concurrent AppHost run.
  `HostGraphFactory.EnsurePortIsFree` only probed `127.0.0.1`, so it passed and Kestrel then failed
  on `[::1]`. Fixes:
  - `InProcTestPorts.DerivedRangeEnd` drops from 40000 to **32768**. Every derived port now stays
    below every default ephemeral range. The cost is 8 derived slots instead of 13, which means
    `-j` is capped at 8 on a derived base. A test pins the bound.
  - `EnsurePortIsFree` now probes IPv6 loopback as well, so a taken port fails with the teaching
    error.
  - The `"response ended prematurely"` / `EndOfStreamException` lines in the run log belong to
    api-server-integration-tests' DCP resource logs. That project passed.
- **Testcontainers start hardening** (`tests/common/timewarp-testing/postgres-test-availability.cs`).
  In local runs, `Database_One_Migration_Behind_Should_Wait_Then_Serve` (web-server-integration-tests)
  failed 3 times in 21 runs on a Postgres container start. Once the first connection was refused,
  and twice Ryuk's initialization timed out. `StartAsync` is now followed by a host-side TCP probe of
  the mapped port, and a start that fails on a Ryuk or port-readiness error is disposed and retried
  once.
- **Testcontainers vs AppHost lane: kept parallel.** I tried an intermediate design that kept
  Testcontainers suites off AppHost starts. A Ryuk timeout still happened with **no AppHost
  running** (only the two Testcontainers suites), and that design added ~30s. The evidence does not
  tie the Testcontainers flake to AppHost starts. It is recorded in the `TestParallelism` Design
  region.
- Docs: `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` port range and
  AppHost lane.

### Proof: 10× integration, locally

All runs use `taskset -c 0-3 dev test --group integration -j 4 -q --no-build`, which limits the
machine to 4 cores. Docker Desktop on WSL.

| | Runs | Result | Test-phase wall time |
|---|---|---|---|
| Before (task 370 dispatcher) | 2 | 2/2 green | 98s, 99s |
| After (final code) | 10 | **10/10 green**, 0 Postgres retries | 168, 145, 140, 142, 143, 144, 152, 145, 148, 143s (mean 147s) |

- The integration job gets about **+45–50s** slower, because the critical path is now the serial
  chain api-server-integration (21s) → aspire-tests (55s) → web-spa-integration (49s).
- On the #515 PR run, integration took 5m18s, playwright 5m55s and template-smoke 7m40s.
  template-smoke stays the CI critical path, so the overall CI wall clock should not move.
- The retry path never fired in the final 10 runs, so it has no direct proof yet.
- The shared AppHost user-secrets file (`a3d4de92…/secrets.json`) has the same hash before and
  after all runs.

### Gates

- `dev build`: 0 warnings, 0 errors.
- dev-cli-tests 252/252, timewarp-testing-tests 14/14.
- Full `dev test`: **23 of 23 projects passed** in 178s (8 workers, port bases 20000–30500). The
  first attempt hung in web-spa-playwright-tests: `Failed to fetch dynamically imported module
  …/_framework/dotnet.js`, the known stale-WASM-asset problem in a reused worktree. `dev clean` +
  `dev build` fixed it, and playwright then passed 17/17. That problem is unrelated to this change.
- `ganda repo audit`: passes.

### How to validate

**Smoke:**
```bash
dev test --group integration -j 4
```
**Expect:** the run prints `AppHost suites run one at a time: api-server-integration-tests,
aspire-tests, web-spa-integration-tests`. No two of those three show overlapping `Started` /
`=====` lines. Port bases are all ≤ 30500. The run ends with `8 of 8 projects passed`.

**Smoke:**
```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release
```
**Expect:** all tests pass, including `BootsAppHost_Given_` (the monorepo set and the source-scan
guard) and `Order_Should_StartAppHostSuitesFirstWithinTheirGroup`.

**Smoke:** after merge into `feature/ci-speed`, push to #515.
**Expect:** `ci (integration)` is green on both the push and PR runs, with the same three-suite
AppHost serialization in its log.

### Review disposition

- Rounds: 1. Effort 2, roster: general (review oracle, claude-opus-5-5).
- Final counts: bug 1 fixed · suggestion 1 fixed · nit 1 wontfix · **0 open**.
- Disposition: **accepted-exceptions**.
  - M1 fixed: the IPv6 port probe no longer reports "in use" when `::1` cannot be bound.
  - M2 fixed: the Postgres port probe has a per-connect timeout, and the container is disposed
    after the final failed attempt.
  - M3 wontfix: the regex also matches a commented-out PackageReference. That false positive only
    serializes a suite.
- Gates after the fixes:
  - `dev build`: 0 warnings, 0 errors.
  - `dev test -q`: 23 of 23 projects passed.
  - `dev test -g integration`: 8 of 8 passed on a fresh build.
  - `dev test -g fast`: 14 of 14 passed.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.
