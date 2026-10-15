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
- [ ] Bounded parallel runner with per-project port-base slots
- [ ] Concurrency hints for Docker-heavy suites
- [ ] Buffered output + summary table; correct exit code
- [ ] Timing before/after; 5× loop green
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
