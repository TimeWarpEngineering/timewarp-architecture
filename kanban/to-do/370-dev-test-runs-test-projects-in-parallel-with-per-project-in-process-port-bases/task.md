# dev test runs test projects in parallel with per-project in-process port bases

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

`test-command.cs` deliberately runs projects one at a time ("Integration suites share one
InProcTestPorts base"). Task 330 made the base overridable and slot-based (20000 + n·1500, 13 slots).

## Requirements
- Run test projects **concurrently** with bounded parallelism (default about the CPU count, and
  `--max-parallel N` to change it). Give each concurrent project its **own
  `TIMEWARP_TEST_PORT_BASE` slot**, so in-process hosts never collide. Closed-box Aspire suites
  already use dynamic ports (319).
- Docker-heavy suites (Testcontainers, Aspire) may need lower concurrency. Allow a per-group or
  per-project concurrency hint, and record the defaults.
- Output stays readable: buffer each project's output and print it whole when that project
  finishes, with a final summary table (project, result, duration). Exit non-zero if any project fails.
- Works with 366's groups (each CI job runs its group in parallel) and 365's `--no-build`.
- Before/after timing locally and in CI; run 5× in a loop to show no port or Docker contention flakes.

## Checklist
- [x] Bounded parallel runner with per-project port-base slots
- [x] Concurrency hints for Docker-heavy suites
- [x] Buffered output + summary table; correct exit code
- [x] Timing before/after (local); 5× loop green. CI after-timing comes from this PR's run.
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15: implemented (ganda task work, implement node).
- Review oracle: claude (claude-opus-5-5), tw-implementation-review round 1, clean — 2026-10-15
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-15T11:24:28Z

## Results

### What changed
- `tools/dev-cli/services/test-parallelism.cs` (new, pure): worker count (default CPU count,
  capped at the port slots: 13 for a derived base), one port base per worker
  (`PortBases`: the checkout's base, then the next derived slots; an override base steps up by
  1500 while the set stays ≤ 65535), group caps, and start order (playwright, integration, fast).
- `tools/dev-cli/endpoints/test-command.cs`: projects run concurrently. Each worker holds one
  `TIMEWARP_TEST_PORT_BASE` while it runs a project. New options `--max-parallel/-j N` and
  `--max-integration N`. Output is buffered per project and printed whole when it finishes
  (`--quiet` prints only failures). A summary table (project, group, result, duration, port base)
  ends the run, and any failure sets exit code 1.
- Recorded defaults: the integration group (Testcontainers, Aspire testing, host graphs) runs at
  most **3** at once (`TestParallelism.DefaultIntegrationMaxParallel`). Fast and playwright have
  no cap of their own.
- Build race fix: concurrent `dotnet test` builds would race on shared `obj/`/`bin/`. A serial
  build phase now runs first: the Release solution build (skipped with `--no-build`, which 365's
  `dev workflow` passes), then each selected out-of-solution project (aggregators,
  timewarp-testing-tests). Every `dotnet test` is `--no-build --no-restore`
  (`DotNetTestArguments.Build()` / `Test()`). `--group` (366) still selects the projects first.
- Playwright: `ask-surface-playwright-tests.cs` reopens the edit-mode menu if it closed before the
  click. Under parallel load the menu closed between the screenshot and the click (a 30s timeout,
  loop 3 of the first 5× run). That was a UI race, not a port or Docker collision.
- Wording that said `dev test` runs projects serially is updated in `HostGraphFactory`
  (the doc comment and the port-in-use message), host-graph-factory-tests, and the
  co-located-jaribu-runfiles skill reference.
- Unit tests: `tests/tools/dev-cli-tests/test-parallelism-tests.cs` (new) and an updated
  `dotnet-test-arguments-tests.cs`. dev-cli-tests compile-include `in-proc-test-ports.cs`.

### Timing (local, 32 cores, `--no-build` unless noted)
| Run | Before (serial) | After (parallel) |
|-----|-----------------|------------------|
| fast group | 77s | 28s (`-j 4`, matching CI's 4 cores) |
| playwright group | 141s | unchanged (one project, about 140s) |
| integration group | 204s | 91s (`-j 4`) |
| whole suite | 422s (sum of the groups) | 154–171s (5 runs); 165s with the solution build |

The single playwright project (about 140s) is now the lower bound for the whole suite.
CI timing for after: read it from this PR's `ci-group` jobs.

### 5× loop
After the Playwright fix: 5 consecutive `dev test --no-build --quiet` runs, 23/23 projects passed
each time (157s, 162s, 154s, 171s, 160s). No port collisions and no Docker contention.

### Known trade-off
The derived ring has 13 slots, so a worker's base can be another checkout's own base. Two worktrees
running `dev test` at the same moment can collide. `EnsurePortIsFree` then fails loudly, naming
the base and `TIMEWARP_TEST_PORT_BASE`, and `-j` lowers the slot use. A possible follow-up is
compact port offsets (fewer ports per base, so more slots).

### Gates
- `dev build`: 0 warnings, 0 errors
- `dev test`: 23/23 (and the 5× loop above)
- `dev template-smoke`: SUCCEEDED (tools/dev-cli ships in generated apps)
- `dev check-version`: version is new
- `ganda repo audit`: passes

### How to validate
Smoke:
```bash
./bin/dev self-install            # bin/dev is stale after dev-cli edits
./bin/dev test --quiet            # whole suite, parallel, with the solution build
./bin/dev test --no-build --group integration -j 4 --max-integration 2
./bin/dev test --no-build -j 0    # rejected: one port base per worker
```
Expect:
- Lines like `Started  <project> (port base NNNNN)` show distinct bases among running projects.
- Each project's output prints as one block (`===== <project>: passed in N.Ns =====`).
- A summary table ends the run with `23 of 23 projects passed.` and exit code 0. A failing project
  shows `FAILED`, prints its output, and the run exits 1.
- The whole suite takes about 2.5–3 min locally, against about 7 min serial.
- `-j 0` prints `--max-parallel must be 1..13 ...` and exits 1.

### Review disposition
- Rounds: 1. Roster: general. Effort: 2 (by-diff, 562 lines).
- Final counts: bug 0, suggestion 1 fixed (M1), nit 0. Open: 0.
- Disposition: **clean**. M1: `RunAllAsync` is now a dispatcher loop. Whenever a port base is
  free it starts the first pending project in start order whose group cap has room. Before this,
  integration projects waiting on the cap could queue behind every fast project.
- After the fix: `dev build` 0/0; `dev test --no-build -j 4 --quiet` 23/23 (163s), with every
  integration project started before any fast one.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

